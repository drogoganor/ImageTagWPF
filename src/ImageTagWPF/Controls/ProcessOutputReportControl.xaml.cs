using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ImageTagWPF.Code;
using ImageTagWPF.Controls.Windows;
using Path = System.IO.Path;

namespace ImageTagWPF.Controls
{
    /// <summary>
    /// Interaction logic for ProcessOutputReportControl.xaml
    /// </summary>
    public partial class ProcessOutputReportControl : UserControl
    {
        protected ProcessOperation Process;
        protected ProcessOutputReport Report;


        public ProcessOutputReportControl()
        {
            InitializeComponent();
        }

        private void ListBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var obj = ListBox.SelectedItem as ProcessOperation;
            if (obj != null)
            {
                Process = obj;

            }
        }

        public void SetProcessReport(ProcessOutputReport report)
        {
            Report = report;
            if (Report != null)
            {
                ListBox.ItemsSource = Report.Operations.Where(x => x.Severity == FileProcessSeverity.Warn);
            }
            else
            {
                ListBox.ItemsSource = null;
            }
        }


        private void AttemptResolutionButton_Click(object sender, RoutedEventArgs e)
        {
            if (App.CheckForProcessingItems())
                return;

            if (Process != null)
            {
                if (Process.Output == ProcessRecommendedOutput.CompareFiles)
                {
                    string result = string.Empty;
                    bool bothImages = false;
                    bool renameOne = false;
                    var ok = ImageCompareWindow.ShowDialog(Process.SourceFilename, Process.DestinationFilename,
                        out result, out bothImages, out renameOne);

                    if (ok && !String.IsNullOrEmpty(result))
                    {
                        result = result.Trim().ToLowerInvariant();
                        if (bothImages)
                        {
                            // Keep both
                            if (renameOne)
                            {
                                var targetImage = App.ImageTag.Entities.Images.FirstOrDefault(x => x.Path.Trim().ToLower() == result);

                                if (targetImage != null && File.Exists(targetImage.Path))
                                {
                                    var suffix = Path.GetExtension(targetImage.Path);
                                    var newPath = targetImage.Path.Substring(0, targetImage.Path.Length - suffix.Length)
                                                  + " " + Path.GetRandomFileName().Substring(0, 6)
                                                  + suffix;
                                    
                                    if (!Util.RetryMove(targetImage.Path, newPath))
                                    {
                                        App.Log.Error("Couldn't move file: " + targetImage.Path + " to " + newPath);
                                        return;
                                    }
                                    
                                    targetImage.Path = newPath;
                                
                                    App.ImageTag.Entities.SaveChanges();

                                    App.Log.Info("Saved renamed file as: " + newPath);
                                }
                            }

                        }
                        else
                        {
                            // Discard one

                            var discardedImageName = result == Process.SourceFilename
                               ? Process.DestinationFilename
                               : Process.SourceFilename;
                            discardedImageName = discardedImageName.Trim().ToLowerInvariant();

                            var targetImage = App.ImageTag.Entities.Images.FirstOrDefault(x => x.Path.Trim().ToLower() == result);
                            var discardedImage = App.ImageTag.Entities.Images.FirstOrDefault(x => x.Path.Trim().ToLower() == discardedImageName);

                            if (targetImage != null && discardedImage != null)
                            {
                                // Consolidate tags
                                foreach (var discardedImageTag in discardedImage.Tags)
                                {
                                    targetImage.Tags.Add(discardedImageTag);
                                }

                                // Rating, too
                                if (!targetImage.Rating.HasValue && discardedImage.Rating.HasValue)
                                {
                                    targetImage.Rating = discardedImage.Rating;
                                }



                                App.ImageTag.Entities.Images.Remove(discardedImage);

                                App.ImageTag.Entities.SaveChanges();

                                App.Log.Info("Removed image from DB: " + discardedImage.Path);
                                
                                try
                                {
                                    File.Delete(discardedImage.Path);
                                    App.Log.Info("Deleted image file: " + discardedImage.Path);
                                }
                                catch (Exception ex)
                                {
                                    App.Log.Error("Error deleting image file: " + discardedImage.Path + ": " + ex.Message);
                                    throw ex;
                                }


                                // Update process report items - remove any with the same target path as our discarded
                                if (Report != null)
                                {
                                    var removeList = new List<ProcessOperation>();
                                    foreach (var processOperation in Report.Operations)
                                    {
                                        if (processOperation.Output == Process.Output)
                                        {
                                            // If the source is the same as the discarded image, remove
                                            if (processOperation.SourceFilename.ToLowerInvariant().Trim() == discardedImageName
                                                || processOperation.DestinationFilename.ToLowerInvariant().Trim() == discardedImageName)
                                            {
                                                removeList.Add(processOperation);
                                            }
                                        }
                                    }

                                    foreach (var processOperation in removeList)
                                    {
                                        Report.Operations.Remove(processOperation);
                                    }

                                }



                            }
                            else
                            {
                                App.Log.Error("Error: Either target image or discarded image couldn't be found: " + result +
                                              ", " + discardedImageName);
                                return;
                            }


                            // Remove ourselves too
                            if (Report != null)
                            {
                                Report.Operations.Remove(Process);
                                Process = null;

                                SetProcessReport(Report);
                            }
                        }

                        
                    }
                }
            }
        }
    }
}

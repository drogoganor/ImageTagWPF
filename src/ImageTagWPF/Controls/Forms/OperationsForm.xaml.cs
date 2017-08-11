using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
using ImageTagWPF.Data;
using Image = ImageTagWPF.Data.Image;
using Path = System.IO.Path;

namespace ImageTagWPF.Controls.Forms
{
    /// <summary>
    /// Interaction logic for OperationsForm.xaml
    /// </summary>
    public partial class OperationsForm : UserControl
    {
        public delegate void OperationsCommenceMoveFilesHandler();

        public event OperationsCommenceMoveFilesHandler OnStartFileMove;
        

        public OperationsForm()
        {
            InitializeComponent();
        }


        public void UpdateOutput()
        {
            AppenderTextBox.ScrollToEnd();

            OutputReportControl.SetProcessReport(App.ImageTag.FileProcessData);
        }


        private void OrganizeImagesButton_Click(object sender, RoutedEventArgs e)
        {
            OnStartFileMove?.Invoke();

            var suppressSuccessMessages = SuppressSuccessMessagesCheckbox.IsChecked.Value;
            var dispatchItem = new ImageTagDispatchItem()
            {
                Action = () => App.ImageTag.OrganizeImages(suppressSuccessMessages),
                Description = "Organizing images",
                Dispatcher = Dispatcher
            };
            App.ImageTag.Enqueue(dispatchItem);

            dispatchItem.OnFinish += DispatchItem_OnFinish;

            //AppenderTextBox.ScrollToEnd();

            //OutputReportControl.ListBox.ItemsSource = FileProcessData.Operations.Where(x => x.Severity == FileProcessSeverity.Error);


        }

        private void DispatchItem_OnFinish(ImageTagDispatchItem self)
        {
            self.OnFinish -= DispatchItem_OnFinish;

            self.Dispatcher.Invoke(UpdateOutput);
        }

        private void FindDupesByContentButton_Click(object sender, RoutedEventArgs e)
        {
            var result =
                MessageBox.Show(
                    "WARNING: This process takes a long time. Are you sure you want to continue?",
                    "Find duplicates by content", MessageBoxButton.OKCancel, MessageBoxImage.Warning);

            if (result != MessageBoxResult.OK)
                return;

            var dispatchItem = new ImageTagDispatchItem()
            {
                //Action = () => App.ImageTag.FindOrphanedFiles(),
                Action = () => App.ImageTag.FindDuplicateFilesByContent(),
                //Action = () => App.ImageTag.CheckFileDupesTest(),
                Description = "Finding duplicate files by content",
                Dispatcher = Dispatcher
            };
            App.ImageTag.Enqueue(dispatchItem);
            dispatchItem.OnFinish += DispatchItem_OnFinish;
        }

        private void FindOrphansButton_Click(object sender, RoutedEventArgs e)
        {
            var dispatchItem = new ImageTagDispatchItem()
            {
                Action = () => App.ImageTag.FindOrphanedFiles(),
                Description = "Finding orphaned files",
                Dispatcher = Dispatcher
            };
            App.ImageTag.Enqueue(dispatchItem);
            dispatchItem.OnFinish += DispatchItem_OnFinish;
        }


        private void FindOrphanedRecordsButton_Click(object sender, RoutedEventArgs e)
        {
            var dispatchItem = new ImageTagDispatchItem()
            {
                Action = () => App.ImageTag.FindOrphanedRecords(),
                Description = "Finding orphaned records",
                Dispatcher = Dispatcher
            };
            App.ImageTag.Enqueue(dispatchItem);
            dispatchItem.OnFinish += DispatchItem_OnFinish;


            //OutputReportControl.ListBox.ItemsSource =
            //    FileProcessData.Operations.Where(x => x.Severity == FileProcessSeverity.Error);

            //AppenderTextBox.ScrollToEnd();


        }

        private void UpdateParentTagsButton_Click(object sender, RoutedEventArgs e)
        {
            var dispatchItem = new ImageTagDispatchItem()
            {
                Action = () => App.ImageTag.UpdateParentTags(),
                Description = "Updating parent tags",
                Dispatcher = Dispatcher
            };
            App.ImageTag.Enqueue(dispatchItem);
            dispatchItem.OnFinish += DispatchItem_OnFinish;

            //AppenderTextBox.ScrollToEnd();
        }

        private void ConsolidateDuplicateFilesButton_Click(object sender, RoutedEventArgs e)
        {
            bool ignoreFilename = IgnoreFilenameCheckbox.IsChecked.Value;
            bool deleteFileToo = DeleteFileCheckbox.IsChecked.Value;

            if (ignoreFilename && deleteFileToo)
            {
                var result =
                    MessageBox.Show(
                        "Are you sure you want to consolidate duplicated image records by checksum and delete duplicate files?",
                        "Consolidate records", MessageBoxButton.OKCancel, MessageBoxImage.Warning);

                if (result != MessageBoxResult.OK)
                    return;
            }

            var dispatchItem = new ImageTagDispatchItem()
            {
                Action = () => App.ImageTag.ConsolidateDuplicateFiles(ignoreFilename, deleteFileToo),
                Description = "Consolidating duplicate files",
                Dispatcher = Dispatcher
            };
            App.ImageTag.Enqueue(dispatchItem);
            dispatchItem.OnFinish += DispatchItem_OnFinish;



            //AppenderTextBox.ScrollToEnd();

        }

        private void ReplaceDirButton_Click(object sender, RoutedEventArgs e)
        {
            var search = SearchDirTextBox.Text; //@"T:\unsorted\Images 2\";
            var replace = ReplaceDirTextBox.Text; //@"T:\Images 2\";

            if (string.IsNullOrEmpty(search))
            {
                MessageBox.Show("You must enter a search value to continue.", "Enter search value", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result =
                MessageBox.Show(
                    "Are you sure you want to replace all instances of image paths beginning with '" + search +
                    "' with '" + replace + "'?",
                    "Replace path", MessageBoxButton.OKCancel, MessageBoxImage.Warning);

            if (result != MessageBoxResult.OK)
                return;

            if (String.IsNullOrEmpty(replace))
            {
                result =
                    MessageBox.Show(
                        "Your replacement term is empty. Are you sure you want to proceed?",
                        "Confirm '' as replace term", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                if (result != MessageBoxResult.OK)
                    return;
            }


            var dispatchItem = new ImageTagDispatchItem()
            {
                Action = () => App.ImageTag.ReplaceDir(search, replace),
                Description = "Replacing directory",
                Dispatcher = Dispatcher
            };
            App.ImageTag.Enqueue(dispatchItem);
            dispatchItem.OnFinish += DispatchItem_OnFinish;

            //AppenderTextBox.ScrollToEnd();
        }

        private void ClearCopyDirectoriesButton_Click(object sender, RoutedEventArgs e)
        {
            var dispatchItem = new ImageTagDispatchItem()
            {
                Action = () => App.ImageTag.ClearCopyDirectories(),
                Description = "Clearing copy directories",
                Dispatcher = Dispatcher
            };
            App.ImageTag.Enqueue(dispatchItem);
            dispatchItem.OnFinish += DispatchItem_OnFinish;
        }

        private void DelistDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            var directory = DelistDirTextBox.Text;

            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
            {
                MessageBox.Show("You must enter a valid directory to continue.", "Enter directory", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result =
                MessageBox.Show(
                    "Are you sure you want to delist all images with this directory from the database? " + Environment.NewLine + directory,
                    "Delist directory", MessageBoxButton.OKCancel, MessageBoxImage.Warning);

            if (result != MessageBoxResult.OK)
                return;
            var dispatchItem = new ImageTagDispatchItem()
            {
                Action = () => App.ImageTag.DelistDirectory(directory),
                Description = "Delisting directory",
                Dispatcher = Dispatcher
            };
            App.ImageTag.Enqueue(dispatchItem);
            dispatchItem.OnFinish += DispatchItem_OnFinish;

        }

        private void ClearLogButton_Click(object sender, RoutedEventArgs e)
        {
            AppenderTextBox.Clear();
        }

    }
}

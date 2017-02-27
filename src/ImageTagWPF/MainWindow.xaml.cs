using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
using System.Windows.Threading;
using ImageTagWPF.Code;
using ImageTagWPF.Data;
using ImageTagWPF.Model;
using Image = ImageTagWPF.Data.Image;
using Path = System.IO.Path;

namespace ImageTagWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<ImageTagDispatchItem> DispatchItems { get; set; }
        public DispatcherTimer TaskTimer;

        public MainWindow()
        {
            InitializeComponent();

            DispatchItems = new ObservableCollection<ImageTagDispatchItem>();

            TaskTimer = new DispatcherTimer();
            TaskTimer.Tick += TaskTimer_Tick;
            TaskTimer.Interval = new TimeSpan(0, 0, 0, 2);
            TaskTimer.Start();
        }

        private void TaskTimer_Tick(object sender, EventArgs e)
        {
            //ProcessingItemsBox.ItemsSource = DispatchItems;
        }
        


        public void Initialize()
        {
            ImageTaggerForm.Initialize();
            TagManager.Initialize();
            ImageExplorerForm.Initialize();
            SettingsForm.Initialize();
            OrganizeForm.Initialize();

            App.ImageTag.OnStartItem += ImageTag_OnStartItem;
            App.ImageTag.OnFinishItem += ImageTag_OnFinishItem;

            ProcessingItemsBox.ItemsSource = DispatchItems;
        }

        private void ImageTag_OnFinishItem(Code.ImageTagDispatchItem item)
        {
            ProcessingItemsBox.Dispatcher.Invoke(() =>
            {
                DispatchItems.Remove(item);
            });
        }

        private void ImageTag_OnStartItem(Code.ImageTagDispatchItem item)
        {
            //ProcessingItemsBox.Dispatcher.Invoke(() =>
            //{
                DispatchItems.Add(item);

            //});
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            Initialize();

            App.ImageTag.Start();
        }

        private void TagManager_OnOnTagsUpdated()
        {
            ImageTaggerForm.TagSelectControl.LoadTags();
            ImageExplorerForm.TagSelectControl.LoadTags();
            OrganizeForm.TagSelectControl.LoadTags();
        }

        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            TaskTimer.Stop();
            App.ImageTag.SaveSettings();
            App.ImageTag.Stop();
            App.ImageTag.OnStartItem -= ImageTag_OnStartItem;
            App.ImageTag.OnFinishItem -= ImageTag_OnFinishItem;
        }

        private void OperationsForm_OnOnStartFileMove()
        {
            ImageTaggerForm.ClearThumbItems();
            ImageExplorerForm.ClearThumbItems();
        }

        
        private void CancelProcessButton_OnClick(object sender, RoutedEventArgs e)
        {
            var label = sender as Button;
            if (label != null && label.Parent != null)
            {
                var dispatch = label.DataContext as ImageTagDispatchItem;
                if (dispatch != null)
                {
                    if (dispatch.IsRunning)
                    {
                        if (dispatch.IsCancelable)
                        {
                            var result =
                                MessageBox.Show(
                                    "Are you sure you want to cancel this process? " + Environment.NewLine + dispatch.Description,
                                    "Cancel running process", MessageBoxButton.OKCancel, MessageBoxImage.Warning);

                            if (result != MessageBoxResult.OK)
                                return;

                            App.CancellationTokenSource.Cancel();
                        }
                    }
                    else
                    {
                        App.ImageTag.Dequeue(dispatch);
                    }
                }
            }
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            if (DispatchItems.Count > 0)
            {
                var result =
                    MessageBox.Show(
                        "Warning: Tagging processes are still running.\r\nPlease wait for these processes to complete, or cancel them.",
                        "Processes still running", MessageBoxButton.OK, MessageBoxImage.Warning);

                //if (result != MessageBoxResult.OK)
                {
                    e.Cancel = true;
                }
            }
        }
    }


    
}

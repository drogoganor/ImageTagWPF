using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using ImageTagWPF.Code;
using ImageTagWPF.Model;

namespace ImageTagWPF.Controls
{
    public delegate void ImageFileSelectedDelegate(ImageFileThumbData data);

    /// <summary>
    /// Interaction logic for FileExplorer.xaml
    /// </summary>
    public partial class FileExplorer : UserControl
    {
        public event ImageFileSelectedDelegate OnFileSelected;

        public int ThumbWidth = 140;
        protected Point DragStart = new Point();
        protected bool StartedDragOnScrollbar = false;

        protected ScrollBar ScrollBar;
        

        public FileExplorer()
        {
            InitializeComponent();

            FileViewer.Loaded += (sender, e) =>
            {
                ScrollViewer scrollViewer = FileViewer.GetVisualChild<ScrollViewer>(); //Extension method
                if (scrollViewer != null)
                {
                    scrollViewer.CanContentScroll = false;

                    var scrollBar = scrollViewer.Template.FindName("PART_VerticalScrollBar", scrollViewer) as ScrollBar;
                    if (scrollBar != null)
                    {
                        ScrollBar = scrollBar;
                        scrollBar.ValueChanged += delegate
                        {
                            ScrollbarValueChanged(scrollViewer);
                        };
                    }


                    scrollViewer.MouseMove += ScrollViewer_MouseMove;
                    scrollViewer.PreviewMouseDown += ScrollViewer_PreviewMouseDown;
                }
            };
        }

        private void ScrollViewer_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            DragStart = e.GetPosition(null);
        }

        private void ScrollViewer_MouseMove(object sender, MouseEventArgs e)
        {
            Point mpos = e.GetPosition(null);
            Vector diff = DragStart - mpos;

            if (e.LeftButton == MouseButtonState.Pressed &&
                Keyboard.IsKeyDown(Key.LeftCtrl) &&
                Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                // right about here you get the file urls of the selected items.
                // should be quite easy, if not, ask.
                //var thisData = DataContext as ImageFileThumbData;

                var fileList = new List<string>();
                var selectedItems = FileViewer.SelectedItems;
                foreach (var selectedItem in selectedItems)
                {
                    var thumbDataObj = selectedItem as ImageFileThumbData;
                    if (thumbDataObj != null)
                    {
                        fileList.Add(thumbDataObj.FullPath);
                    }
                }

                string[] files = fileList.ToArray();
                string dataFormat = DataFormats.FileDrop;
                DataObject dataObject = new DataObject(dataFormat, files);
                DragDrop.DoDragDrop(this, dataObject, DragDropEffects.Copy);
            }
        }

        protected void ScrollbarValueChanged(ScrollViewer scrollViewer)
        {
            // Get first item height
            WrapPanel element = FileViewer.GetVisualChild<WrapPanel>();

            if (FileViewer.Items.Count == 0)
                return;

            var visualItem = FileViewer.GetVisualChild<ListBoxItem>();

            if (visualItem == null || visualItem.ActualHeight == 0 || visualItem.ActualWidth == 0)
                return;

            int numRows = (int)(element.ActualHeight / visualItem.ActualHeight);
            int numColumns = (int)(element.ActualWidth / visualItem.ActualWidth);
            
            int firstVisibleIndex =
                (int)(scrollViewer.VerticalOffset / visualItem.ActualHeight) * numColumns;
            firstVisibleIndex -= firstVisibleIndex % numColumns;

            int numVisibleRows =
                (int)Math.Ceiling(scrollViewer.ViewportHeight / visualItem.ActualHeight);

            int lastVisibleIndex = firstVisibleIndex + (numVisibleRows * numColumns) + numColumns;
            
            if (firstVisibleIndex < 0)
                firstVisibleIndex = 0;
            if (firstVisibleIndex >= FileViewer.Items.Count)
                firstVisibleIndex = FileViewer.Items.Count - 1;

            if (lastVisibleIndex < 0)
                lastVisibleIndex = 0;
            if (lastVisibleIndex >= FileViewer.Items.Count)
                lastVisibleIndex = FileViewer.Items.Count - 1;

            var visibleItems = new List<ImageFileThumbData>();
            for (int i = firstVisibleIndex; i <= lastVisibleIndex; i++)
            {
                var item = FileViewer.Items[i] as ImageFileThumbData;

                if (item != null && item.ImageSource == null)
                {
                    visibleItems.Add(item);
                }
            }

            var dispatcher = Dispatcher.CurrentDispatcher;

            //var threadStarter = new ThreadStart(() =>
            //   LoadThumbnailsForItems(dispatcher, visibleItems));
            //threadStarter.Invoke();

            //Task.Run(() => {
                LoadThumbnailsForItems(dispatcher, visibleItems);
            //});

            //var task = new Task(() => LoadThumbnailsForItems(visibleItems));
            //task.Start();
        }


        public void LoadFirst(int n)
        {
            if (FileViewer.Items.Count == 0)
                return;

            if (n > FileViewer.Items.Count)
                n = FileViewer.Items.Count;

            ImageFileThumbData firstItem = null;
            var visibleItems = new List<ImageFileThumbData>();
            for (int i = 0; i < n; i++)
            {
                var item = FileViewer.Items[i] as ImageFileThumbData;

                if (item != null && item.ImageSource == null)
                {
                    if (firstItem == null)
                        firstItem = item;
                    visibleItems.Add(item);
                }

            }


            var dispatcher = Dispatcher.CurrentDispatcher;
            
            LoadThumbnailsForItems(dispatcher, visibleItems);
            //var task = new Task(() => LoadThumbnailsForItems(visibleItems));
            //task.Start();

            if (firstItem != null)
                FileViewer.ScrollIntoView(firstItem);
        }


        protected void LoadThumbnailsForItems(Dispatcher dispatcher, List<ImageFileThumbData> list)
        {
            foreach (var item in list)
            {
                if (item.ImageSource == null)
                {
                    BitmapImage thumbImage = Util.GetThumbnailForImage(item.FullPath, ThumbWidth);
                    if (thumbImage != null)
                    {
                        item.ImageSource = thumbImage;
                    }

                }
            }

            //dispatcher.Invoke(() =>
            //{
                SetThumbnailList(list);
            //});

        }


        protected void SetThumbnailList(List<ImageFileThumbData> list)
        {
            foreach (var item in list)
            {
                var visItem = (ListBoxItem)(FileViewer.ItemContainerGenerator.ContainerFromItem(item));
                var thumbItem = visItem.GetVisualChild<ImageThumbnailControl>();
                if (thumbItem == null)
                    continue;

                if (item.ImageSource != null)
                {
                    thumbItem.ThumbnailImage.Source = item.ImageSource;
                }
            }
        }

        private void FileViewer_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateWindowThumbnails();
        }

        public void UpdateWindowThumbnails()
        {
            ScrollViewer scrollViewer = FileViewer.GetVisualChild<ScrollViewer>(); //Extension method
            if (scrollViewer != null)
            {
                ScrollbarValueChanged(scrollViewer);
            }
        }

        public void UpdateExplorerWindowPath(string targetDirectory)
        {
            if (Directory.Exists(targetDirectory))
            {
                // Get all files in this directory
                var files = Util.GetFilesByExtensions(new DirectoryInfo(targetDirectory), App.ImageTag.Settings.Extensions.Select(x => x.Extension).ToArray());

                /*
                var list = new List<ImageFileThumbData>();


                foreach (var file in files)
                {
                    var filenameOnly = System.IO.Path.GetFileName(file.Name);
                    var directory = System.IO.Path.GetDirectoryName(file.DirectoryName);

                    var fullPath = file.FullName;

                    BitmapImage thumbImage = Util.GetThumbnailForImage(fullPath, thumbWidth);

                    var imageFile = new ImageFileThumbData()
                    {
                        Filename = filenameOnly,
                        FullPath = fullPath,
                        ImageSource = thumbImage
                    };

                    list.Add(imageFile);
                }*/

                var newFiles = files.Select(x => new ImageFileThumbData() {Filename = x.Name, FullPath = x.FullName});


                App.Current.Dispatcher.Invoke(() =>
                {
                    FileViewer.ItemsSource = newFiles;

                    if (FileViewer.Items.Count > 0)
                        FileViewer.ScrollIntoView(FileViewer.Items[0]);

                    LoadFirst(60);

                    FileViewer.IsEnabled = true;
                });
            }
        }
        

        private void FileViewer_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = FileViewer.SelectedItems;
            if (selected.Count > 0)
            {
                var first = selected[0];
                var thumbObj = first as ImageFileThumbData;

                if (thumbObj != null)
                {
                    if (thumbObj.Image == null)
                    {
                        // Find out if it's indexed
                        var indexedImage = App.ImageTag.Entities.Images.FirstOrDefault(x =>
                                x.Path == thumbObj.FullPath
                        //&& x.Checksum == checksum
                        );

                        // Add our record to the thumb data
                        if (indexedImage != null)
                        {
                            thumbObj.Image = indexedImage;
                        }
                    }


                    OnFileSelected?.Invoke(thumbObj);
                    
                }
            }
        }


        public void ClearThumbs()
        {
            FileViewer.ItemsSource = null;
            UpdateLayout();
        }

        private void FileViewer_OnMouseMove(object sender, MouseEventArgs e)
        {

        }

        private void FileViewer_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
        }

    }
}

using System;
using System.Collections;
using System.Collections.Generic;
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
using ImageTagWPF.Model;

namespace ImageTagWPF.Controls.Forms
{
    /// <summary>
    /// Interaction logic for ImageExplorerForm.xaml
    /// </summary>
    public partial class ImageExplorerForm : UserControl
    {
        protected int ThumbWidth = 200;

        public List<TagModel> SearchTermList = new List<TagModel>();

        protected Point DragStart = new Point();
        public bool IsEditMode { get { return EditModCheckBox.IsChecked.Value; } }
        public bool AnyMatch { get { return AnyMatchCheckbox.IsChecked.Value; } }


        protected bool MultipleSelected = false;
        protected bool IsUpdating = false;
        protected List<TagModel> LastSelectedTags = new List<TagModel>();


        public ImageExplorerForm()
        {
            InitializeComponent();

            TagSelectControl.RatingControl.IsEnabled = false;
        }

        public void Initialize()
        {
            TagSelectControl.Initialize();
        }

        public void ClearThumbItems()
        {
            ImageExplorer.FileViewer.ItemsSource = null;
        }


        private void TagSelectControl_OnOnTagAdd(TagModel selecteditem)
        {
            if (!IsEditMode)
            {
                SearchTermList.Add(selecteditem);
                SearchTermsBox.ItemsSource = SearchTermList;
            }
            else
            {
                // Add new tags!
                var imageList = new List<ImageFileThumbData>();

                var selected = ImageExplorer.FileViewer.SelectedItems;
                foreach (var selectedItem in selected)
                {
                    var tempItem = selectedItem as ImageFileThumbData;
                    if (tempItem != null)
                        imageList.Add(tempItem);
                }

                Util.UpdateImageTags(imageList, new List<TagModel>() { selecteditem}, App.CancellationTokenSource.Token, addOnly:true);

                try
                {
                    App.ImageTag.Entities.SaveChanges();
                }
                catch (Exception ex)
                {
                    App.Log.Error("Couldn't tag images: " + ex.Message);
                    throw ex;
                }
            }
        }

        private void TagSelectControl_OnOnSelectionChanged(List<TagModel> tags)
        {
            if (IsUpdating)
                return;

            if (!IsEditMode)
            {
                // If not edit then add to search terms
                
                SearchTermList.Clear();
                foreach (var tag in tags)
                {
                    var tagModelItem = tag as TagModel;
                    if (tagModelItem != null)
                    {
                        //var newTerm = new SearchTermModel()
                        //{
                        //    Tag = tagModelItem
                        //};

                        SearchTermList.Add(tagModelItem);
                    }
                }

                SearchTermsBox.ItemsSource = tags;
            }
            else
            {
                // Add new tags!
                
                var imageList = new List<ImageFileThumbData>();


                var selected = ImageExplorer.FileViewer.SelectedItems;
                foreach (var selectedItem in selected)
                {
                    var tempItem = selectedItem as ImageFileThumbData;
                    if (tempItem != null)
                        imageList.Add(tempItem);
                }
            
                if (MultipleSelected)
                {
                    var tagIDs = tags.Select(x => (int)x.Tag.ID).ToArray();

                    // Tags removed
                    var removedTags = LastSelectedTags.Where(x => !tagIDs.Contains((int)x.Tag.ID)).ToList();

                    Util.RemoveImageTags(imageList, removedTags);
                    Util.UpdateImageTags(imageList, tags, App.CancellationTokenSource.Token, addOnly: true);

                    LastSelectedTags = tags;
                }
                else
                    Util.UpdateImageTags(imageList, tags, App.CancellationTokenSource.Token);

                try
                {
                    App.ImageTag.Entities.SaveChanges();
                }
                catch (Exception ex)
                {
                    App.Log.Error("Couldn't tag images: " + ex.Message);
                    throw ex;
                }


                // Update recent tag box?
                TagSelectControl.UpdateRecentTags();
            }
        }


        public void WildcardSearch(string term)
        {
            var text = term.Trim().ToLowerInvariant();
            if (!String.IsNullOrEmpty(text))
            {
                // Just split string for now
                var searchTags = text.Split(new[] { ' ' });

                // Get matching tags
                var matchingTags = App.ImageTag.Entities.Tags.Where(x => searchTags.Contains(x.Name));

                // Any image that has all of the tags fulfilled
                var searchCollection = App.ImageTag.Entities.Images
                    .Where(images =>
                    matchingTags.All(t =>
                                images.Tags.Any(mt => mt.ID == t.ID)));

                var thumbDataCollection = new List<ImageFileThumbData>();
                foreach (var image in searchCollection)
                {
                    var newThumbData = new ImageFileThumbData()
                    {
                        Checksum = image.Checksum,
                        Filename = image.Path,
                        FullPath = image.Path,
                        Image = image,
                    };

                    //var thumbImage = Util.GetThumbnailForImage(image.Path, ThumbWidth);
                    //newThumbData.ImageSource = thumbImage;


                    thumbDataCollection.Add(newThumbData);
                }

                ImageExplorer.FileViewer.ItemsSource = thumbDataCollection;
            }
        }

        private void SearchButton_OnClick(object sender, RoutedEventArgs e)
        {
            // Get matching tags
            var matchingTags = SearchTermList.Select(x => (int)x.Tag.ID).ToArray();

            var thumbDataCollection = new List<ImageFileThumbData>();

            int minRating = RatingControl.Rating;
            bool unratedOnly = UnratedOnlyCheckbox.IsChecked.Value;

            // Any image that has all of the tags fulfilled
            var searchCollection = App.ImageTag.Entities.Images

                // Tags AND/OR filtering
                .Where(images =>
                (!AnyMatch && matchingTags.All(t => images.Tags.Any(mt => (int)mt.ID == t))) || 
                (AnyMatch && matchingTags.Any(t => images.Tags.Any(mt => (int)mt.ID == t))))

                // Minimum rating filter
                .Where(images => (unratedOnly && !images.Rating.HasValue) ||
                (!unratedOnly && ((minRating == 0 && !images.Rating.HasValue) || (int)images.Rating.Value >= minRating))  // Only those with a rating >= min
                );

            // Develop thumbnail results
            foreach (var image in searchCollection)
            {
                var newThumbData = new ImageFileThumbData()
                {
                    Checksum = image.Checksum,
                    Filename = image.Path,
                    FullPath = image.Path,
                    Image = image,
                };

                //var thumbImage = Util.GetThumbnailForImage(image.Path, ThumbWidth);
                //newThumbData.ImageSource = thumbImage;

                thumbDataCollection.Add(newThumbData);
            }

            ImageExplorer.FileViewer.ItemsSource = thumbDataCollection;

            ImageExplorer.LoadFirst(60);

            
        }

        private void ClearSearchButton_OnClick(object sender, RoutedEventArgs e)
        {
            SearchTermsBox.ItemsSource = null;
            
            TagSelectControl.TagViewer.UnselectAll();

            RatingControl.SetRating(0);
        }

        private void ClearTagButton_OnClick(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
            {
                var senderTagObject = button.DataContext as TagModel;

                //var senderTagObject = sender.Parent as TagModel;
                if (senderTagObject != null)
                {
                    if (SearchTermList.Contains(senderTagObject))
                    {
                        SearchTermList.Remove(senderTagObject);
                    }



                    TagSelectControl.TagViewer.SelectedItems.Remove(senderTagObject);
                }


            }
        }

        private void EditModCheckBox_OnChecked(object sender, RoutedEventArgs e)
        {
            SearchButton.IsEnabled = false;
            SearchTermsBox.IsEnabled = false;
            ClearSearchButton.IsEnabled = false;
            AnyMatchCheckbox.IsEnabled = false;
            TagSelectControl.RatingControl.IsEnabled = true;

            ReselectCurrentImage();
        }

        private void EditModCheckBox_OnUnchecked(object sender, RoutedEventArgs e)
        {
            SearchButton.IsEnabled = true;
            SearchTermsBox.IsEnabled = true;
            ClearSearchButton.IsEnabled = true;
            AnyMatchCheckbox.IsEnabled = true;
            TagSelectControl.RatingControl.IsEnabled = false;
            
            TagSelectControl.TagViewer.UnselectAll();
        }


        private void ReselectCurrentImage()
        {
            // Reload data in edit mode by selecting this again
            var selectedItems = ImageExplorer.FileViewer.SelectedItems;
            foreach (var selected in selectedItems)
            {
                ImageExplorer.FileViewer.UnselectAll();
                ImageExplorer.FileViewer.SelectedItems.Add(selected);
                break;
            }
        }



        private void ImageExplorer_OnOnFileSelected(ImageFileThumbData data)
        {
            if (IsEditMode)
            {
                // If selecting in edit mode, populate tags
                TagSelectControl.SetImageData(data);

                TagSelectControl.DisableTagViewerSelectionChange();

                if (ImageExplorer.FileViewer.SelectedItems.Count > 1)
                {
                    IsUpdating = true;

                    MultipleSelected = true;

                    TagSelectControl.TagViewer.UnselectAll();
                    LastSelectedTags.Clear();
                    
                    var thumbItems = ImageExplorer.FileViewer.SelectedItems;
                    var list = new List<ImageFileThumbData>();
                    foreach (var thumbItem in thumbItems)
                    {
                        list.Add(thumbItem as ImageFileThumbData);
                    }

                    LastSelectedTags = TagSelectControl.SetCommonTags(list);

                    IsUpdating = false;
                }
                else
                    MultipleSelected = false;

                TagSelectControl.EnableTagViewerSelectionChange();
            }
            else
            {
                TagSelectControl.SetRatingData(data);
            }
        }


        private void DragArea_OnMouseMove(object sender, MouseEventArgs e)
        {
            Point mpos = e.GetPosition(null);
            Vector diff = DragStart - mpos;

            if (e.LeftButton == MouseButtonState.Pressed &&
                //Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance &&
                //Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                diff.Length > SystemParameters.MinimumVerticalDragDistance)
            {
                DragDropCurrentSelection();
            }
        }

        private void DragAreaShuffle_OnMouseMove(object sender, MouseEventArgs e)
        {
            Point mpos = e.GetPosition(null);
            Vector diff = DragStart - mpos;

            if (e.LeftButton == MouseButtonState.Pressed &&
                diff.Length > SystemParameters.MinimumVerticalDragDistance)
            {
                DragDropCurrentSelection(true);
            }
        }

        private void DragArea_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            DragStart = e.GetPosition(null);
        }


        protected void DragDropCurrentSelection(bool shuffle = false)
        {
            var fileList = new List<string>();
            var selectedItems = ImageExplorer.FileViewer.SelectedItems;
            foreach (var selectedItem in selectedItems)
            {
                var thumbDataObj = selectedItem as ImageFileThumbData;
                if (thumbDataObj != null)
                {
                    fileList.Add(thumbDataObj.FullPath);
                }
            }

            if (shuffle)
                fileList.Shuffle();

            string[] files = fileList.ToArray();
            string dataFormat = DataFormats.FileDrop;
            DataObject dataObject = new DataObject(dataFormat, files);
            DragDrop.DoDragDrop(this, dataObject, DragDropEffects.Copy);
        }

        private void TagSelectControl_OnOnRatingChanged(int rating)
        {
            if (IsEditMode)
            {
                // Add rating
                var imageList = new List<ImageFileThumbData>();

                var selected = ImageExplorer.FileViewer.SelectedItems;
                foreach (var selectedItem in selected)
                {
                    var tempItem = selectedItem as ImageFileThumbData;
                    if (tempItem != null)
                        imageList.Add(tempItem);
                }

                Util.UpdateImageRating(imageList, rating);

                try
                {
                    App.ImageTag.Entities.SaveChanges();
                }
                catch (Exception ex)
                {
                    App.Log.Error("Couldn't update rating of images: " + ex.Message);
                    throw ex;
                }
            }
        }
    }
}

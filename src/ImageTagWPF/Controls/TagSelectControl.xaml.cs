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
using ColorPickerWPF.Code;
using ImageTagWPF.Code;
using ImageTagWPF.Data;
using ImageTagWPF.Model;

namespace ImageTagWPF.Controls
{
    /// <summary>
    /// Interaction logic for TagSelectControl.xaml
    /// </summary>
    public partial class TagSelectControl : UserControl
    {
        public delegate void TagSelectionChangedHandler(List<TagModel> selectedItems);
        public delegate void TagSelectionAddedHandler(TagModel selectedItem);

        public event TagSelectionChangedHandler OnSelectionChanged;
        public event TagSelectionAddedHandler OnTagAdd;
        public event StarRatingControl.StarRatingChangedHandler OnRatingChanged;

        public List<TagModel> AllTags = new List<TagModel>();

        public bool IsUpdating = false;
        
        public TagSelectControl()
        {
            InitializeComponent();

        }

        public void Initialize()
        {
            LoadTags();
        }


        public void LoadTags()
        {
            AllTags = GetAllTagModels();
            TagViewer.ItemsSource = AllTags;

            var recentTags = FilterToRecentlyUsed(AllTags).ToList();
            TagViewerRecent.ItemsSource = recentTags;
        }
        

        protected List<TagModel> GetAllTagModels()
        {
            var tags = App.ImageTag.Entities.Tags
                .Select(x => new TagModel()
                {
                    Tag = x
                })
                .OrderBy(x => x.Tag.TagType)
                .ThenBy(x => x.Tag.Name)
                .ToList();

            foreach (var tagModel in tags)
            {
                var type = (TagType)tagModel.Tag.TagType;
                tagModel.HexColor = App.ImageTag.GetColorForTagType(type).ToHexString();
            }
            return tags;
        }

        protected IEnumerable<TagModel> FilterByCategory(IEnumerable<TagModel> tags)
        {
            // Filter by category
            if (!DescriptorCheckBox.IsChecked.Value ||
                !SubjectCheckBox.IsChecked.Value ||
                !EventCheckBox.IsChecked.Value ||
                !SeriesCheckBox.IsChecked.Value ||
                !ArtistCheckBox.IsChecked.Value)
            {
                tags = tags
                    .Where(x =>
                            (TagType)x.Tag.TagType == TagType.Descriptor && DescriptorCheckBox.IsChecked.Value ||
                            (TagType)x.Tag.TagType == TagType.Subject && SubjectCheckBox.IsChecked.Value ||
                            (TagType)x.Tag.TagType == TagType.Event && EventCheckBox.IsChecked.Value ||
                            (TagType)x.Tag.TagType == TagType.Series && SeriesCheckBox.IsChecked.Value ||
                            (TagType)x.Tag.TagType == TagType.Artist && ArtistCheckBox.IsChecked.Value
                    );
            }
            return tags;
        }

        protected IEnumerable<TagModel> FilterToRecentlyUsed(IEnumerable<TagModel> tags)
        {
            tags = tags.Where(x => x.Tag.LastUsed.HasValue)
                //.OrderBy(x => (TagType) x.Tag.TagType)
                .OrderByDescending(x => x.Tag.LastUsed.Value);
            return tags;
        }

        public void UpdateRecentTags()
        {
            var list = TagViewer.ItemsSource as IEnumerable<TagModel>;
            var r = FilterToRecentlyUsed(list);
            
            TagViewerRecent.ItemsSource = r;
        }


        public void SearchTags(string searchTerm)
        {
            var searchLower = searchTerm.Trim().ToLowerInvariant();

            IEnumerable<TagModel> tagModelList = AllTags;

            if (!String.IsNullOrEmpty(searchLower))
            {
                tagModelList = tagModelList
                    .Where(y => y.Tag.Name.ToLower().Contains(searchTerm));
            }

            // Filter by category
            tagModelList = FilterByCategory(tagModelList);
            
            TagViewer.ItemsSource = tagModelList;

            UpdateRecentTags();
        }


        public void SetImageData(ImageFileThumbData data)
        {
            DisableTagViewerSelectionChange();

            SetRatingData(data);

            TagViewer.UnselectAll();

            //TagViewer.SelectedItems.Clear();

            // If we have an image record, load selected tags
            if (data.Image != null)
            {
                var tagIDs = data.Image.Tags.Select(x => (int)x.ID);

                var itemList = AllTags;
                if (itemList != null)
                {
                    foreach (var imageTag in itemList)
                    {
                        if (tagIDs.Contains((int)imageTag.Tag.ID))
                        {
                            TagViewer.SelectedItems.Add(imageTag);
                        }
                    }
                }

            }

            EnableTagViewerSelectionChange();
        }

        public List<TagModel> SetCommonTags(List<ImageFileThumbData> images)
        {
            //DisableTagViewerSelectionChange();
            
            RatingControl.SetRating(0);

            TagViewer.UnselectAll();

            var results = new List<TagModel>();

            if (images.Any(x => x.Image == null)) // If we haven't even tagged this one, show nothing!
            {
                EnableTagViewerSelectionChange();
                return results;
            }


            var allImages = images.Where(x => x.Image != null).ToList();
            /*
            var allTags = images.Where(x => x.Image != null).SelectMany(x => x.Image.Tags).Distinct();
            var allTagIDs = images.Where(x => x.Image != null).SelectMany(x => x.Image.Tags).Distinct().Select(x => (int)x.ID).ToArray();

            var commonTags = allTags.Where(x => allImages.All(y => y.Image.Tags.Any(z => allTagIDs.Contains((int) z.ID)))).ToList();
            */



            if (allImages.Count > 0)
            {
                IEnumerable<Tag> firstTags = allImages[0].Image.Tags;

                for (int i = 1; i < allImages.Count; i++)
                {
                    var image = allImages[i];
                    var tags = image.Image.Tags;

                    firstTags = firstTags.Intersect(tags);
                }

                var commonTagIDs = firstTags.Select(x => (int) x.ID).ToArray();
                results = AllTags.Where(x => commonTagIDs.Contains((int) x.Tag.ID)).ToList();

                foreach (var imageTag in results)
                {
                    TagViewer.SelectedItems.Add(imageTag);
                }
            }

            
            //EnableTagViewerSelectionChange();

            return results;
        }

        public void SetRatingData(ImageFileThumbData data)
        {
            RatingControl.SetRating(0);

            if (data.Image != null)
            {
                if (data.Image.Rating.HasValue)
                {
                    RatingControl.SetRating((int)data.Image.Rating);
                }
            }
        }



        private void TagViewer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsUpdating)
            {
                var tagList = new List<TagModel>();
                foreach (var selectedTag in TagViewer.SelectedItems)
                {
                    tagList.Add(selectedTag as TagModel);
                }

                OnSelectionChanged?.Invoke(tagList);

                UpdateRecentTags();
            }
        }

        

        private void TagViewerRecent_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var tagList = new List<TagModel>();
            foreach (var selectedTag in TagViewerRecent.SelectedItems)
            {
                tagList.Add(selectedTag as TagModel);
            }

            if (tagList.Count > 0)
            {
                OnTagAdd?.Invoke(tagList[0]);

                TagViewerRecent.UnselectAll();
            }
            
        }


        public void DisableTagViewerSelectionChange()
        {
            IsUpdating = true;
        }
        public void EnableTagViewerSelectionChange()
        {
            IsUpdating = false;
        }

        private void FilterTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            var text = FilterTextBox.Text;
            SearchTags(text);
        }

        private void ClearButton_OnClick(object sender, RoutedEventArgs e)
        {
            FilterTextBox.Text = string.Empty;
            SearchTags(string.Empty);
        }

        private void CheckBox_OnChanged(object sender, RoutedEventArgs e)
        {
            if (TagViewer != null) // shunt to prevent this being triggered on initialization
            {
                var text = FilterTextBox.Text;
                SearchTags(text);
            }

        }

        private void RatingControl_OnOnRatingChanged(int rating)
        {
            OnRatingChanged?.Invoke(rating);
        }
    }
}

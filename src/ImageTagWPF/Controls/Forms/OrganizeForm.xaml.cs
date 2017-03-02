using System;
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

namespace ImageTagWPF.Controls.Forms
{
    /// <summary>
    /// Interaction logic for OrganizeForm.xaml
    /// </summary>
    public partial class OrganizeForm : UserControl
    {
        public List<TagModel> SearchTermList = new List<TagModel>();

        public OrganizeForm()
        {
            InitializeComponent();

            ForeColorPickerRow.SetColor(Colors.Black);
        }

        public void Initialize()
        {
            OrganizeTree.Initialize();
            TagSelectControl.Initialize();
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



        private void TagSelectControl_OnOnTagAdd(TagModel selecteditem)
        {
            SearchTermList.Add(selecteditem);
            SearchTermsBox.ItemsSource = SearchTermList;
        }

        private void TagSelectControl_OnOnSelectionChanged(List<TagModel> tags)
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


        private void TagSelectControl_OnOnRatingChanged(int rating)
        {
            // TODO: Implement some kind of rating filter
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = OrganizeTree.FileTree.SelectedItem as OrganizeDirectory;
            if (selectedItem != null && !String.IsNullOrEmpty(NameTextBox.Text))
            {
                selectedItem.Name = NameTextBox.Text;
                selectedItem.CopyOnly = CopyOnlyCheckbox.IsChecked.Value ? 1 : 0;
                selectedItem.IgnoreParent = IgnoreParentCheckbox.IsChecked.Value ? 1 : 0;
                selectedItem.OrTags = OrTagsCheckbox.IsChecked.Value ? 1 : 0;
                selectedItem.TheseTagsOnly = TheseTagsOnlyCheckbox.IsChecked.Value ? 1 : 0;
                selectedItem.ForeColor = ForeColorPickerRow.Color.ToHexString();

                if (TagSelectControl.RatingControl.Rating > 0)
                {
                    selectedItem.Rating = TagSelectControl.RatingControl.Rating;
                }
                else
                {
                    selectedItem.Rating = null;
                }

                var tags = new List<Tag>();
                foreach (var item in SearchTermsBox.Items)
                {
                    var tagItem = item as TagModel;
                    if (tagItem != null)
                    {
                        tags.Add(tagItem.Tag);
                    }
                }
                selectedItem.Tags = tags;

                App.ImageTag.Entities.SaveChanges();
                
                OrganizeTree.Initialize(); // refresh content

                OrganizeTree.FileTree.SelectItem(selectedItem);
            }
        }

        private void AddNewButton_Click(object sender, RoutedEventArgs e)
        {
            // Add as child
            var newDir = new OrganizeDirectory()
            {
                Name = NameTextBox.Text,
                CopyOnly = CopyOnlyCheckbox.IsChecked.Value ? 1 : 0,
                IgnoreParent = IgnoreParentCheckbox.IsChecked.Value ? 1 : 0,
                OrTags = OrTagsCheckbox.IsChecked.Value ? 1 : 0,
                TheseTagsOnly = TheseTagsOnlyCheckbox.IsChecked.Value ? 1 : 0,
                ForeColor = ForeColorPickerRow.Color.ToHexString(),
                //BackColor = BackColorPickerRow.Color.ToHexString(),
            };

            var tags = new List<Tag>();
            foreach (var item in SearchTermsBox.Items)
            {
                var tagItem = item as TagModel;
                if (tagItem != null)
                {
                    tags.Add(tagItem.Tag);
                }
            }
            newDir.Tags = tags;

            if (TagSelectControl.RatingControl.Rating > 0)
            {
                newDir.Rating = TagSelectControl.RatingControl.Rating;
            }


            var selectedItem = OrganizeTree.FileTree.SelectedItem as OrganizeDirectory;
            if (selectedItem != null && !String.IsNullOrEmpty(NameTextBox.Text))
            {
                // Add as child of selected
                selectedItem.ChildDirectories.Add(newDir);
            }
            else
            {
                App.ImageTag.Entities.OrganizeDirectories.Add(newDir);
            }
            
            App.ImageTag.Entities.SaveChanges();

            OrganizeTree.Initialize(); // refresh content

            OrganizeTree.FileTree.SelectItem(newDir);
        }

        private void OrganizeTree_OnOnPickNode(object item)
        {
            var treeNode = item as OrganizeDirectory;
            if (treeNode != null)
            {
                NameTextBox.Text = treeNode.Name;
                IgnoreParentCheckbox.IsChecked = treeNode.IgnoreParent != 0;
                CopyOnlyCheckbox.IsChecked = treeNode.CopyOnly != 0;
                OrTagsCheckbox.IsChecked = treeNode.OrTags != 0;
                TheseTagsOnlyCheckbox.IsChecked = treeNode.TheseTagsOnly != 0;


                if (treeNode.Rating.HasValue)
                {
                    TagSelectControl.RatingControl.SetRating((int) treeNode.Rating);
                }
                else
                {
                    TagSelectControl.RatingControl.SetRating(0);
                }

                ForeColorPickerRow.SetColor((Color)ColorConverter.ConvertFromString(treeNode.ForeColor));
                //BackColorPickerRow.SetColor((Color)ColorConverter.ConvertFromString(treeNode.BackColor));
                
                TagSelectControl.TagViewer.UnselectAll();

                SearchTermList.Clear();
                var list = new List<TagModel>();
                foreach (var tag in treeNode.Tags)
                {
                    if (tag != null)
                    {
                        var tagModel = new TagModel()
                        {
                            HexColor = Colors.Black.ToHexString(),
                            Tag = tag
                        };
                        list.Add(tagModel);
                        SearchTermList.Add(tagModel);
                    }
                }
                
                SearchTermsBox.ItemsSource = list;

            }
        }

        private void ClearTagsButton_Click(object sender, RoutedEventArgs e)
        {
            SearchTermList.Clear();

            SearchTermsBox.ItemsSource = SearchTermList;
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = OrganizeTree.FileTree.SelectedItem as OrganizeDirectory;
            if (selectedItem != null)
            {
                if (selectedItem.ChildDirectories.Count > 0)
                {
                    MessageBox.Show("Cannot delete this item because it has child directories.");
                    return;
                }

                var result =
                MessageBox.Show(
                    "Are you sure you want to delete this organize directory? The actual directory is left intact.",
                    "Delete organize directory", MessageBoxButton.OKCancel, MessageBoxImage.Question);

                if (result == MessageBoxResult.OK)
                {
                    App.ImageTag.Entities.OrganizeDirectories.Remove(selectedItem);
                    
                    App.ImageTag.Entities.SaveChanges();

                    OrganizeTree.Initialize(); // refresh content
                }
            }
        }

        private void OrganizeTree_OnOnNodeMoveUp()
        {
            MoveNode(true);

            App.ImageTag.Entities.SaveChanges();

            var selectedItem = OrganizeTree.FileTree.SelectedItem as OrganizeDirectory;

            OrganizeTree.Initialize(); // refresh content

            OrganizeTree.FileTree.SelectItem(selectedItem);
        }

        private void OrganizeTree_OnOnNodeMoveDown()
        {
            MoveNode(false);

            App.ImageTag.Entities.SaveChanges();
            
            var selectedItem = OrganizeTree.FileTree.SelectedItem as OrganizeDirectory;

            OrganizeTree.Initialize(); // refresh content

            OrganizeTree.FileTree.SelectItem(selectedItem);
        }

        protected void MoveNode(bool up)
        {
            var selectedItem = OrganizeTree.FileTree.SelectedItem as OrganizeDirectory;
            if (selectedItem != null)
            {
                // get parent
                if (selectedItem.ParentDirectories.Count > 0)
                {
                    var parentDir = selectedItem.ParentDirectories.First();

                    int ourIndex = 0;
                    foreach (var childDir in parentDir.ChildDirectories)
                    {
                        if (childDir.ID == selectedItem.ID)
                        {
                            if (up)
                            {
                                ourIndex--;
                            }
                            else
                            {
                                ourIndex++;
                            }
                            break;
                        }
                        ourIndex++;
                    }

                    if (ourIndex < 0)
                        return;

                    if (ourIndex >= parentDir.ChildDirectories.Count)
                        return;

                    var list = parentDir.ChildDirectories.ToList();

                    var newItem = list[ourIndex];


                    SwapOrganizeDirectory(selectedItem, newItem);
                    

                    parentDir.ChildDirectories = list;


                    OrganizeTree.FileTree.SelectItem(newItem);


                }

            }
        }


        protected void SwapOrganizeDirectory(OrganizeDirectory dir1, OrganizeDirectory dir2)
        {
            // Swap 
            var tempDir = new OrganizeDirectory()
            {
                BackColor = dir2.BackColor,
                ChildDirectories = dir2.ChildDirectories.ToList(),
                Tags = dir2.Tags.ToList(),
                Name = dir2.Name,
                IgnoreParent = dir2.IgnoreParent,
                OrTags = dir2.OrTags,
                CopyOnly = dir2.CopyOnly,
                Rating = dir2.Rating,
                Description = dir2.Description,
                ForeColor = dir2.ForeColor,
                TheseTagsOnly = dir2.TheseTagsOnly
            };

            dir2.BackColor = dir1.BackColor;
            dir2.ForeColor = dir1.ForeColor;
            dir2.ChildDirectories = dir1.ChildDirectories.ToList();
            dir2.Tags = dir1.Tags.ToList();
            dir2.Name = dir1.Name;
            dir2.IgnoreParent = dir1.IgnoreParent;
            dir2.OrTags = dir1.OrTags;
            dir2.CopyOnly = dir1.CopyOnly;
            dir2.Rating = dir1.Rating;
            dir2.Description = dir1.Description;
            dir2.TheseTagsOnly = dir1.TheseTagsOnly;


            dir1.BackColor = tempDir.BackColor;
            dir1.ForeColor = tempDir.ForeColor;
            dir1.ChildDirectories = tempDir.ChildDirectories.ToList();
            dir1.Tags = tempDir.Tags.ToList();
            dir1.Name = tempDir.Name;
            dir1.IgnoreParent = tempDir.IgnoreParent;
            dir1.OrTags = tempDir.OrTags;
            dir1.CopyOnly = tempDir.CopyOnly;
            dir1.Rating = tempDir.Rating;
            dir1.Description = tempDir.Description;
            dir1.TheseTagsOnly = tempDir.TheseTagsOnly;
        }

        protected void MoveNodeAllTheWay(bool up)
        {
            var selectedItem = OrganizeTree.FileTree.SelectedItem as OrganizeDirectory;
            if (selectedItem != null)
            {
                // get parent
                if (selectedItem.ParentDirectories.Count > 0)
                {
                    var parentDir = selectedItem.ParentDirectories.First();

                    int ourIndex = 0;
                    if (!up)
                    {
                        ourIndex = parentDir.ChildDirectories.Count - 1;
                    }
                    
                    var list = parentDir.ChildDirectories.ToList();

                    var newItem = list[ourIndex];

                    if (newItem == selectedItem || newItem.ID == selectedItem.ID)
                        return;

                    SwapOrganizeDirectory(selectedItem, newItem);
                    parentDir.ChildDirectories = list;
                    OrganizeTree.FileTree.SelectItem(newItem);
                }

            }
        }

        private void OrganizeTree_OnOnNodeMoveTop()
        {
            MoveNodeAllTheWay(true);


            //App.ImageTag.Entities.SaveChanges();

            var selectedItem = OrganizeTree.FileTree.SelectedItem as OrganizeDirectory;

            OrganizeTree.Initialize(); // refresh content

            OrganizeTree.FileTree.SelectItem(selectedItem);
        }

        private void OrganizeTree_OnOnNodeMoveBottom()
        {
            MoveNodeAllTheWay(false);


            //App.ImageTag.Entities.SaveChanges();

            var selectedItem = OrganizeTree.FileTree.SelectedItem as OrganizeDirectory;

            OrganizeTree.Initialize(); // refresh content

            OrganizeTree.FileTree.SelectItem(selectedItem);
        }
    }
}

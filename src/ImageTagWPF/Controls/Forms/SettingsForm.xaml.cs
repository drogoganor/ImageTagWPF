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
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ImageTagWPF.Code;
using MessageBox = System.Windows.MessageBox;
using Path = System.IO.Path;
using UserControl = System.Windows.Controls.UserControl;

namespace ImageTagWPF.Controls.Forms
{
    /// <summary>
    /// Interaction logic for SettingsForm.xaml
    /// </summary>
    public partial class SettingsForm : UserControl
    {
        //public ImageTagSettings Settings;

        public SettingsForm()
        {
            InitializeComponent();
        }

        public void Initialize()
        {
            var settings = App.ImageTag.Settings;

            if (settings != null)
            {
                DefaultDirectoryTextBox.Text = settings.DefaultDirectory;

                CategoryColorsListBox.ItemsSource = settings.TagCategoryColors.ToArray();

                ExtensionListBox.ItemsSource = settings.FileExtensions.ToArray();
            }
        }

        private void SaveButton_OnClick(object sender, RoutedEventArgs e)
        {
            var settings = App.ImageTag.Settings;

            if (settings != null)
            {
                settings.DefaultDirectory = DefaultDirectoryTextBox.Text;

                var categoryColors = new List<TagCategoryColor>();
                foreach (var item in CategoryColorsListBox.Items)
                {
                    var colorItem = item as TagCategoryColor;
                    if (colorItem != null)
                    {
                        categoryColors.Add(colorItem);
                    }
                }

                settings.TagCategoryColors = categoryColors;

                var exts = new List<string>();
                foreach (var item in ExtensionListBox.Items)
                {
                    var ext = item as string;
                    if (ext != null)
                    {
                        exts.Add(ext);
                    }
                }

                settings.FileExtensions = exts;

                var path = Path.Combine(Environment.CurrentDirectory, ImageTag.SettingsName);
                settings.SaveToXml(path);
            }
        }

        private void SaveExtensionButton_Click(object sender, RoutedEventArgs e)
        {
            var extText = ExtensionTextBox.Text.Trim();
            if (!String.IsNullOrEmpty(extText))
            {
                var settings = App.ImageTag.Settings;
                // Find all in list
                var extList = settings.FileExtensions.ToArray();//ExtensionListBox.ItemsSource as IList<string>;
                if (extList != null)
                {
                    var selString = ExtensionListBox.SelectedValue as string;

                    if (selString != extText)
                    {
                        // It's a new one
                        // Verify it's not in list
                        if (extList.All(x => x != extText))
                        {
                            // Not in list, add new
                            settings.FileExtensions.Add(extText);

                            ExtensionListBox.ItemsSource = settings.FileExtensions;


                        }
                        else
                        {
                            MessageBox.Show("Extension already exists in list.");
                        }

                    }
                }


            }
        }

        private void DeleteExtensionButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (ExtensionListBox.SelectedValue != null)
            {
                var settings = App.ImageTag.Settings;

                var selString = ExtensionListBox.SelectedValue as string;
                settings.FileExtensions.Remove(selString);

                ExtensionListBox.ItemsSource = settings.FileExtensions.ToArray();
            }

        }

        private void DirectoryPickButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new FolderBrowserDialog()
            {
                Description = "Select root directory",
                SelectedPath = Environment.CurrentDirectory
            };
            var result = dialog.ShowDialog();

            if (result == DialogResult.OK &&
                Directory.Exists(dialog.SelectedPath))
            {
                DefaultDirectoryTextBox.Text = dialog.SelectedPath;
            }
        }

        private void ExtensionListBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selText = ExtensionListBox.SelectedItem as string;

            ExtensionTextBox.Text = selText;
        }

        private void CategoryColorsListBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selItem = CategoryColorsListBox.SelectedItem as TagCategoryColor;
            if (selItem != null)
            {
                CategoryLabel.Content = selItem.TagType.ToString();
                ColorPickRow.SetColor(selItem.FontColor);
            }
        }
    }
}

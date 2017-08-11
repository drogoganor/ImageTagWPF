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
                this.PortableCheckBox.IsChecked = settings.PortableMode;

                CategoryColorsListBox.ItemsSource = settings.TagCategoryColors.ToArray();

                ExtensionListBox.ItemsSource = settings.Extensions.ToArray();
            }
        }

        private void SaveButton_OnClick(object sender, RoutedEventArgs e)
        {
            var settings = App.ImageTag.Settings;

            if (settings != null)
            {
                settings.DefaultDirectory = DefaultDirectoryTextBox.Text;
                settings.PortableMode = this.PortableCheckBox.IsChecked.Value;
                
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

                var exts = new List<ImageExtensionViewerProgram>();
                foreach (var item in ExtensionListBox.Items)
                {
                    var ext = item as ImageExtensionViewerProgram;
                    if (ext != null)
                    {
                        exts.Add(ext);

                        /*
                        string program = string.Empty;
                        if (Util.TryGetRegisteredApplication(ext.Substring(1), out program))
                        {
                            extPrograms.Add(Util.TryGetRegisteredApplication();
                        }*/

                    }
                }

                settings.Extensions = exts;

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


                var matchingExtension = settings.Extensions.FirstOrDefault(x => x.Extension == extText);
                if (matchingExtension != null)
                {
                    matchingExtension.ViewerProgram = this.ViewerProgramTextBox.Text;
                }
                else
                {
                    // None, add new

                    var program = String.Empty;
                    bool programOK = Util.TryGetRegisteredApplication(extText.Substring(1), out program);

                    settings.Extensions.Add(new ImageExtensionViewerProgram()
                    {
                        Extension = extText,
                        ViewerProgram = (programOK ? program : string.Empty)
                    });

                    ExtensionListBox.ItemsSource = settings.Extensions.ToArray();
                }


            }
        }

        private void DeleteExtensionButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (ExtensionListBox.SelectedValue != null)
            {
                var settings = App.ImageTag.Settings;

                var selString = ExtensionListBox.SelectedValue as ImageExtensionViewerProgram;
                settings.Extensions.Remove(selString);

                ExtensionListBox.ItemsSource = settings.Extensions.ToArray();
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
            var selText = ExtensionListBox.SelectedItem as ImageExtensionViewerProgram;

            ExtensionTextBox.Text = selText.Extension;
            this.ViewerProgramTextBox.Text = selText.ViewerProgram;
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

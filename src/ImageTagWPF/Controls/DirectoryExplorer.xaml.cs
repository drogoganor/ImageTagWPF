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
using ImageTagWPF.Model;
using Path = System.IO.Path;
using UserControl = System.Windows.Controls.UserControl;

namespace ImageTagWPF.Controls
{
    public delegate void DirectoryPickHanlder(string directory);

    /// <summary>
    /// Interaction logic for DirectoryExplorer.xaml
    /// </summary>
    public partial class DirectoryExplorer : UserControl
    {
        public event DirectoryPickHanlder OnPickDirectory;

        protected List<DirectoryModel> RootList;
        protected DirectoryModel Root;

        protected bool SkipNextDirPick = false;
        protected DirectoryModel CurrentDirectory;
        

        public DirectoryExplorer()
        {
            InitializeComponent();
        }


        public void SetRootDirectory(string rootDir)
        {
            DirectoryTextBox.Text = rootDir;

            if (Directory.Exists(rootDir))
            {
                Root = new DirectoryModel()
                {
                    Name = rootDir,
                    FullPath = rootDir
                };

                // Get immediate directories
                ExpandDirectory(Root);

                RootList = new List<DirectoryModel>()
                {
                    Root
                };

                FileTree.ItemsSource = RootList;

                FileTree.SelectItem(Root);
            }
        }




        public void ExpandDirectory(DirectoryModel model)
        {
            if (model.ChildDirectories.Count > 0)
                return; // already expanded

            var directories = Directory.GetDirectories(model.FullPath);

            foreach (var directory in directories)
            {
                var justThisFolder = new DirectoryInfo(directory).Name;

                var child = new DirectoryModel()
                {
                    Name = justThisFolder,
                    FullPath = directory
                };
                model.ChildDirectories.Add(child);
            }

            //FileTree.SelectItem(model);
        }

        private void PickButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new FolderBrowserDialog()
            {
                Description = "Select root directory"
            };
            var result = dialog.ShowDialog();

            if (result == DialogResult.OK &&
                Directory.Exists(dialog.SelectedPath))
            {
                SetRootDirectory(dialog.SelectedPath);
            }
        }

        private void DirectoryTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            var path = DirectoryTextBox.Text;

            if (!String.IsNullOrEmpty(path) && Directory.Exists(path))
            {
                SetRootDirectory(path);
            }
        }

        private void FileTree_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var selectedItem = FileTree.SelectedItem;

            if (SkipNextDirPick)
            {
                SkipNextDirPick = false;
                return;
            }

            var dirModel = selectedItem as DirectoryModel;
            if (dirModel != null)
            {
                if (CurrentDirectory == null || CurrentDirectory != dirModel)
                {
                    CurrentDirectory = dirModel;
                    OnPickDirectory?.Invoke(dirModel.FullPath);
                }

            }

        }
               

        private void UIElement_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            var selectedItem = FileTree.SelectedItem;

            var dirModel = selectedItem as DirectoryModel;
            if (dirModel != null && dirModel.ChildDirectories.Count == 0)
            {
                //ExpandDirectory(dirModel);

                //OnPickDirectory?.Invoke(dirModel.FullPath);

            }
        }

        private void OnItemMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var selectedItem = FileTree.SelectedItem;

            var dirModel = selectedItem as DirectoryModel;
            if (dirModel != null)
            {
                if (dirModel.ChildDirectories.Count == 0)
                {
                    ExpandDirectory(dirModel);

                    RootList = new List<DirectoryModel>()
                    {
                        Root
                    };

                    SkipNextDirPick = true;
                    FileTree.ItemsSource = RootList;

                    FileTree.SelectItem(dirModel);

                }

            }
        }
    }
}

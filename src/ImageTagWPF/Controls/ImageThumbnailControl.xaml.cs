using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using ImageTagWPF.Model;
using Path = System.IO.Path;

namespace ImageTagWPF.Controls
{
    /// <summary>
    /// Interaction logic for ImageThumbnailControl.xaml
    /// </summary>
    public partial class ImageThumbnailControl : UserControl
    {
        public ImageThumbnailControl()
        {
            InitializeComponent();
        }

        private void ImageThumbnailControl_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var file = this.DataContext as ImageFileThumbData;
            if (file != null && File.Exists(file.FullPath))
            {
                var programRecord = App.ImageTag.Settings.Extensions.FirstOrDefault(x => file.FullPath.EndsWith(x.Extension.Substring(1)));
                if (programRecord != null && File.Exists(programRecord.ViewerProgram))
                {
                    if (!String.IsNullOrEmpty(programRecord.ViewerProgram))
                        System.Diagnostics.Process.Start(programRecord.ViewerProgram, file.FullPath);
                    else
                        System.Diagnostics.Process.Start(file.FullPath);
                }
            }
        }

        private void ExploreToMenu_OnClick(object sender, RoutedEventArgs e)
        {
            var file = this.DataContext as ImageFileThumbData;
            if (file != null && File.Exists(file.FullPath))
            {
                string argument = "/select, \"" + file.FullPath + "\"";

                Process.Start("explorer.exe", argument);
            }
        }
    }
}

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
                if (!String.IsNullOrEmpty(App.ImageTag.Settings.DefaultImageViewProgram))
                    System.Diagnostics.Process.Start(App.ImageTag.Settings.DefaultImageViewProgram, file.FullPath);
                else
                    System.Diagnostics.Process.Start(file.FullPath);
            }
        }
    }
}

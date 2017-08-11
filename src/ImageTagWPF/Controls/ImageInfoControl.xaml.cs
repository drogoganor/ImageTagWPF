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
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ImageTagWPF.Code;
using ImageTagWPF.Model;

namespace ImageTagWPF.Controls
{
    /// <summary>
    /// Interaction logic for ImageInfoControl.xaml
    /// </summary>
    public partial class ImageInfoControl : UserControl
    {
        public delegate void ImageInfoSelectHandler();

        public event ImageInfoSelectHandler OnPick;

        protected int ThumbWidth = 200;

        public string Path;

        public ImageInfoControl()
        {
            InitializeComponent();
        }

        public void SetImageSource(string filename)
        {
            if (File.Exists(filename))
            {
                Path = filename;

                
                BitmapImage thumbImage = Util.GetThumbnailForImageSafe(filename, ThumbWidth);
                if (thumbImage != null)
                {
                    thumbImage.Freeze();
                    Image.Source = thumbImage;
                }


                var img = System.Drawing.Image.FromFile(filename);

                var info = new FileInfo(filename);

                var checksum = Util.GetFileHashSHA1(filename);

                PathTextBlock.Text = filename;
                ChecksumTextBlock.Text = checksum;
                DateCreatedTextBlock.Text = info.CreationTime.ToString("G");
                DateModifiedTextBlock.Text = info.LastWriteTime.ToString("G");
                SizeTextBlock.Text = GetFileSizeFriendly(info.Length);
                DimensionsTextBlock.Text = img.Width + " x " + img.Height;

            }
        }


        public void Clear()
        {
            Image.Source = null;
            UpdateLayout();
        }


        public string GetFileSizeFriendly(long bytes)
        {
            int divisor = 1024;
            double length = bytes;

            if (length < divisor)
            {
                return length + " B";
            }
            length /= divisor;
            if (length < divisor)
            {
                return length.ToString("N") + " KB";
            }
            length /= divisor;
            if (length < divisor)
            {
                return length.ToString("N") + " MB";
            }
            length /= divisor;
            if (length < divisor)
            {
                return length.ToString("N") + " GB";
            }
            length /= divisor;
            if (length < divisor)
            {
                return length.ToString("N") + " TB";
            }
            length /= divisor;
            if (length < divisor)
            {
                return length.ToString("N") + " PB";
            }

            return "Gigantic";
        }

        private void AcceptButton_OnClick(object sender, RoutedEventArgs e)
        {
            OnPick?.Invoke();
        }

        private void OpenImageButton_Click(object sender, RoutedEventArgs e)
        {
            var file = PathTextBlock.Text;
            if (!String.IsNullOrEmpty(file) && File.Exists(file))
            {
                var programRecord = App.ImageTag.Settings.Extensions.FirstOrDefault(x => file.EndsWith(x.Extension.Substring(1)));
                if (programRecord != null && File.Exists(programRecord.ViewerProgram))
                {
                    if (!String.IsNullOrEmpty(programRecord.ViewerProgram))
                        System.Diagnostics.Process.Start(programRecord.ViewerProgram, file);
                    else
                        System.Diagnostics.Process.Start(file);
                }

            }
        }
    }
}

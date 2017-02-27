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
using System.Windows.Shapes;

namespace ImageTagWPF.Controls.Windows
{
    /// <summary>
    /// Interaction logic for ImageCompareWindow.xaml
    /// </summary>
    public partial class ImageCompareWindow : Window
    {
        public delegate void ImageComparePickHandler(string result);
        
        public event ImageComparePickHandler OnPick;

        protected static string ResultImage = string.Empty;
        protected bool KeepBoth = false;

        public ImageCompareWindow()
        {
            InitializeComponent();
        }

        private void Image1_OnOnPick()
        {
            OnPick?.Invoke(Image1.Path);
            Close();
        }

        private void Image2_OnOnPick()
        {
            OnPick?.Invoke(Image2.Path);
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            Image1.Clear();
            Image2.Clear();
            UpdateLayout();

            base.OnClosed(e);
        }


        public static bool ShowDialog(string image1, string image2, out string result, out bool keepBoth)
        {
            var window = new ImageCompareWindow();
            window.Image1.SetImageSource(image1);
            window.Image2.SetImageSource(image2);
            window.OnPick += Window_OnPick;

            result = string.Empty;
            ResultImage = result;

            if (window.ShowDialog().HasValue)
            {
                result = ResultImage;
                keepBoth = window.KeepBoth;
                return true;
            }
            window.OnPick -= Window_OnPick;

            keepBoth = window.KeepBoth;

            return false;
        }

        private static void Window_OnPick(string result)
        {
            ResultImage = result;
        }

        private void KeepBothButton_OnClick(object sender, RoutedEventArgs e)
        {
            OnPick?.Invoke(Image2.Path);
            KeepBoth = true;
            Close();
        }
    }
}

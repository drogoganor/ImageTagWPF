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
        protected string ResultImage = string.Empty;
        protected bool KeepBoth = false;
        protected bool RenameOne = false;

        public ImageCompareWindow()
        {
            InitializeComponent();
        }

        private void Image1_OnOnPick()
        {
            ResultImage = Image1.Path;
            
            Close();
        }

        private void Image2_OnOnPick()
        {
            ResultImage = Image2.Path;

            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            Image1.Clear();
            Image2.Clear();
            UpdateLayout();

            base.OnClosed(e);
        }


        public static bool ShowDialog(string image1, string image2, out string result, out bool keepBoth, out bool renameOne)
        {
            var window = new ImageCompareWindow();
            window.Image1.SetImageSource(image1);
            window.Image2.SetImageSource(image2);

            result = string.Empty;

            if (window.ShowDialog().HasValue)
            {
                result = window.ResultImage;
                keepBoth = window.KeepBoth;
                renameOne = window.RenameOne;
                return true;
            }

            keepBoth = window.KeepBoth;
            renameOne = window.RenameOne;

            return false;
        }

        private static void Window_OnPick(string result, bool keepBoth, bool renameOne)
        {
        }

        private void KeepBothButton_OnClick(object sender, RoutedEventArgs e)
        {
            ResultImage = Image2.Path;
            KeepBoth = true;
            RenameOne = AutoRenameCheckbox.IsChecked.Value;
            
            Close();
        }
    }
}

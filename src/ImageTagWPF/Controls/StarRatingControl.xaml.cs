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

namespace ImageTagWPF.Controls
{
    /// <summary>
    /// Interaction logic for StarRatingControl.xaml
    /// </summary>
    public partial class StarRatingControl : UserControl
    {
        public delegate void StarRatingChangedHandler(int rating);

        public event StarRatingChangedHandler OnRatingChanged;

        protected const int IconSize = 24;
        protected const int FullSize = 120;
        protected const int MaxRating = 5;
        protected const int ClickOffset = 19;

        public int Rating = 0;

        public StarRatingControl()
        {
            InitializeComponent();
        }

        private void UIElement_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (IsEnabled)
            {
                var mousePos = e.GetPosition(this);

                // get rating
                double percent = (mousePos.X + ClickOffset) / FullSize;
                int rating = (int)(MaxRating * percent);

                int oldRating = Rating;
                Rating = rating;
                PoliceRatingBounds();
                if (Rating != oldRating)
                {
                    OnRatingChanged?.Invoke(Rating);
                }

                RatingBar.Width = Rating * IconSize;

            }
        }

        public void SetRating(int rating)
        {
            Rating = rating;
            PoliceRatingBounds();

            RatingBar.Width = Rating * IconSize;
        }

        protected void PoliceRatingBounds()
        {
            if (Rating < 0)
                Rating = 0;
            if (Rating > MaxRating)
                Rating = MaxRating;
        }
    }
}

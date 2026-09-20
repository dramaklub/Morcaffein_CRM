using System.ComponentModel;
using CRM_Morcaffein.Theme;

namespace CRM_Morcaffein.winforms.Views.Controls
{
    /// <summary>Simple 1-5 click-to-rate star control. No hover preview yet — click sets the value directly.</summary>
    public partial class StarRatingControl : UserControl
    {
        private int _rating;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int Rating
        {
            get => _rating;
            set
            {
                var clamped = Math.Clamp(value, 0, 5);
                if (_rating == clamped)
                    return;

                _rating = clamped;
                Redraw();
                RatingChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler? RatingChanged;

        public StarRatingControl()
        {
            InitializeComponent();

            foreach (var star in _stars)
            {
                star.Click += (s, e) => Rating = (int)((Label)s!).Tag!;
                star.MouseEnter += (s, e) => Preview((int)((Label)s!).Tag!);
                star.MouseLeave += (s, e) => Redraw();
            }
        }

        private void Preview(int hoverValue)
        {
            for (int i = 0; i < _stars.Length; i++)
            {
                var filled = i < hoverValue;
                _stars[i].Text = filled ? "★" : "☆";
                _stars[i].ForeColor = filled ? BrandPalette.Brand : BrandPalette.RatingInactive;
            }
        }

        private void Redraw() => Preview(_rating);
    }
}

using CRM_Morcaffein.Theme;

namespace CRM_Morcaffein.winforms.Views.Controls
{
    partial class StarRatingControl
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
                components.Dispose();
            base.Dispose(disposing);
        }

        private Label[] _stars;

        private void InitializeComponent()
        {
            SuspendLayout();

            _stars = new Label[5];
            for (int i = 0; i < 5; i++)
            {
                var star = new Label
                {
                    Text = "☆",
                    AutoSize = false,
                    Size = new Size(28, 28),
                    Location = new Point(i * 30, 0),
                    Font = new Font("Segoe UI", 16f, FontStyle.Regular),
                    TextAlign = ContentAlignment.MiddleCenter,
                    ForeColor = BrandPalette.RatingInactive,
                    Cursor = Cursors.Hand,
                    Tag = i + 1,
                };
                _stars[i] = star;
                Controls.Add(star);
            }

            Size = new Size(150, 28);
            ResumeLayout(false);
        }
    }
}

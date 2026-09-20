using CRM_Morcaffein.Theme;

namespace CRM_Morcaffein.winforms.Views.Controls
{
    /// <summary>
    /// Reusable pagination bar: a small "◀ Page X of Y ▶" control.
    /// Host grids wire it up via the PageChanged event and update
    /// UpdatePageInfo(totalCount, currentPage, pageSize) after each fetch.
    /// </summary>
    public class PaginationControl : UserControl
    {
        private Button _prevBtn = null!;
        private Button _nextBtn = null!;
        private Label _pageInfoLabel = null!;

        private int _currentPage = 1;
        private int _totalPages = 1;

        /// <summary>Raised when the user clicks Prev or Next. Carries the new page number.</summary>
        public event EventHandler<int>? PageChanged;

        public int CurrentPage => _currentPage;

        public PaginationControl()
        {
            Dock = DockStyle.Bottom;
            Height = 36;
            BackColor = BrandPalette.SurfacePrimary;
            Padding = new Padding(8, 4, 8, 4);

            _prevBtn = new Button
            {
                Text = "◀",
                Width = 36,
                Height = 28,
                FlatStyle = FlatStyle.Flat,
                BackColor = BrandPalette.ButtonSecondaryFill,
                ForeColor = BrandPalette.ButtonSecondaryText,
                Font = BrandPalette.BrandFontRegular(9.5f),
                Cursor = Cursors.Hand,
                Enabled = false,
            };
            _prevBtn.FlatAppearance.BorderSize = 0;
            _prevBtn.Click += (s, e) => GoTo(_currentPage - 1);

            _nextBtn = new Button
            {
                Text = "▶",
                Width = 36,
                Height = 28,
                FlatStyle = FlatStyle.Flat,
                BackColor = BrandPalette.ButtonSecondaryFill,
                ForeColor = BrandPalette.ButtonSecondaryText,
                Font = BrandPalette.BrandFontRegular(9.5f),
                Cursor = Cursors.Hand,
                Enabled = false,
            };
            _nextBtn.FlatAppearance.BorderSize = 0;
            _nextBtn.Click += (s, e) => GoTo(_currentPage + 1);

            _pageInfoLabel = new Label
            {
                Text = "Page 1 of 1",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = BrandPalette.BrandFontRegular(9f),
                ForeColor = BrandPalette.NeutralTextMuted,
            };

            Controls.Add(_pageInfoLabel);
            Controls.Add(_prevBtn);
            Controls.Add(_nextBtn);

            // Left-docked Prev button, right-docked Next button, fill label between.
            _prevBtn.Dock = DockStyle.Left;
            _nextBtn.Dock = DockStyle.Right;

            ButtonStyler.TrackEnabledState(_prevBtn);
            ButtonStyler.TrackEnabledState(_nextBtn);
        }

        /// <summary>
        /// Called by the host after each fetch. Updates the page text and button states.
        /// </summary>
        public void UpdatePageInfo(int totalCount, int currentPage, int pageSize)
        {
            _currentPage = currentPage;
            _totalPages = pageSize <= 0 ? 1 : Math.Max(1, (int)Math.Ceiling((double)totalCount / pageSize));

            _pageInfoLabel.Text = $"Page {_currentPage} of {_totalPages}";
            _prevBtn.Enabled = _currentPage > 1;
            _nextBtn.Enabled = _currentPage < _totalPages;
        }

        /// <summary>Resets the pager to page 1 (useful when the search filter changes).</summary>
        public void ResetToFirstPage()
        {
            _currentPage = 1;
            _pageInfoLabel.Text = "Page 1 of 1";
            _prevBtn.Enabled = false;
            _nextBtn.Enabled = false;
        }

        private void GoTo(int page)
        {
            if (page < 1 || page > _totalPages) return;
            if (page == _currentPage) return;

            PageChanged?.Invoke(this, page);
        }
    }
}
using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.Theme;

namespace CRM_Morcaffein.winforms.Views.Controls
{
    /// <summary>
    /// Basic-tier Admin dashboard card: a compact roster of the most recently
    /// added employees. Same visual language as VisitFrequencyControl — title,
    /// subtitle, self-painted list rows.
    ///
    /// Clicking anywhere on a row raises RowClicked with the EmployeeId, so
    /// the host view can navigate to the Manage Users tab (matching the
    /// Employees pod click behavior).
    /// </summary>
    public class RecentEmployeesControl : Panel
    {
        public event EventHandler<int>? RowClicked;

        private readonly Label _titleLabel;
        private readonly Label _subtitleLabel;
        private readonly Label _statusLabel;
        private readonly Panel _listHost;

        private const int RowHeight = 44;
        private const int RowGap = 4;

        private static readonly Font NameFont = BrandPalette.BrandFontBold(10f);
        private static readonly Font RoleFont = BrandPalette.BrandFontRegular(9f);

        public RecentEmployeesControl()
        {
            Dock = DockStyle.Fill;
            MinimumSize = new Size(100, 100);
            Margin = new Padding(3);
            BackColor = BrandPalette.SurfacePrimary;
            Padding = new Padding(16, 12, 16, 12);

            _titleLabel = new Label
            {
                Text = "Recent Employees",
                Dock = DockStyle.Top,
                Height = 24,
                Font = BrandPalette.BrandFontBold(11f),
                ForeColor = BrandPalette.NeutralText,
            };

            _subtitleLabel = new Label
            {
                Text = "Newest additions to your team",
                Dock = DockStyle.Top,
                Height = 20,
                Font = BrandPalette.BrandFontRegular(8.5f),
                ForeColor = BrandPalette.NeutralTextMuted,
            };

            _statusLabel = new Label
            {
                Text = "Loading…",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = BrandPalette.BrandFontRegular(9.5f),
                ForeColor = BrandPalette.NeutralTextMuted,
                Visible = false,
            };

            _listHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 6, 0, 0),
            };

            Controls.Add(_listHost);
            Controls.Add(_statusLabel);
            Controls.Add(_subtitleLabel);
            Controls.Add(_titleLabel);
        }

        public void BindData(RecentEmployeesDto data)
        {
            _listHost.Controls.Clear();

            if (data == null || data.Employees.Count == 0)
            {
                _statusLabel.Text = "No employees yet.";
                _statusLabel.Visible = true;
                _listHost.Visible = false;
                return;
            }

            _statusLabel.Visible = false;
            _listHost.Visible = true;

            var y = 0;
            foreach (var emp in data.Employees)
            {
                var row = BuildRow(emp);
                row.Location = new Point(0, y);
                row.Width = _listHost.ClientSize.Width;
                _listHost.Controls.Add(row);
                y += RowHeight + RowGap;
            }

            _listHost.Resize -= ListHost_Resize;
            _listHost.Resize += ListHost_Resize;
        }

        private void ListHost_Resize(object? sender, EventArgs e)
        {
            foreach (Control c in _listHost.Controls)
                c.Width = _listHost.ClientSize.Width;
        }

        private Panel BuildRow(RecentEmployeeItem emp)
        {
            var row = new Panel
            {
                Height = RowHeight,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
            };

            row.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                var w = row.ClientSize.Width;
                if (w <= 0) return;

                // Left: name (top) + role · branch (bottom)
                var nameRect = new Rectangle(0, 6, w - 100, 18);
                TextRenderer.DrawText(g, emp.FullName, NameFont, nameRect,
                    BrandPalette.NeutralText,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

                var roleText = string.IsNullOrWhiteSpace(emp.BranchName)
                    ? emp.Role
                    : $"{emp.Role}  ·  {emp.BranchName}";

                var roleRect = new Rectangle(0, 24, w - 100, 16);
                TextRenderer.DrawText(g, roleText, RoleFont, roleRect,
                    BrandPalette.NeutralTextMuted,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

                // Right: date joined
                var dateText = emp.CreatedAt.ToLocalTime().ToString("MMM d");
                var dateRect = new Rectangle(w - 90, 6, 90, 34);
                TextRenderer.DrawText(g, dateText, RoleFont, dateRect,
                    BrandPalette.NeutralTextMuted,
                    TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
            };

            row.Click += (s, e) => RowClicked?.Invoke(this, emp.EmployeeId);
            return row;
        }
    }
}
using CRM_Morcaffein.Theme;

namespace CRM_Morcaffein.winforms.Views.Controls
{
    public enum DateRangePreset
    {
        Today,
        SevenDays,
        OneMonth,
        Custom
    }

    public class DashboardDateRangeEventArgs : EventArgs
    {
        public DateRangePreset Preset { get; }
        public DateTime StartDate { get; }
        public DateTime EndDate { get; }

        public DashboardDateRangeEventArgs(DateRangePreset preset, DateTime start, DateTime end)
        {
            Preset = preset;
            StartDate = start;
            EndDate = end;
        }
    }

    /// <summary>
    /// Toggleable date-range filter control with soft-neumorphic button pills.
    /// Defaults to 1 Month.
    /// </summary>
    public class DashboardDateRangeFilterControl : UserControl
    {
        public event EventHandler<DashboardDateRangeEventArgs>? RangeChanged;

        public DateRangePreset SelectedPreset { get; private set; } = DateRangePreset.OneMonth;
        public DateTime StartDate { get; private set; }
        public DateTime EndDate { get; private set; }

        private readonly Button _btnToday;
        private readonly Button _btn7Days;
        private readonly Button _btn1Month;
        private readonly Button _btnCustom;
        private readonly DateTimePicker _dtpStart;
        private readonly DateTimePicker _dtpEnd;
        private readonly Panel _customPickerPanel;

        public DashboardDateRangeFilterControl()
        {
            Height = 36;
            AutoSize = true;
            BackColor = Color.Transparent;

            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                BackColor = Color.Transparent,
            };

            var label = new Label
            {
                Text = "📅 Range:",
                AutoSize = true,
                Font = BrandPalette.BrandFontBold(9f),
                ForeColor = BrandPalette.NeutralTextMuted,
                Margin = new Padding(0, 8, 8, 0),
            };

            _btnToday = CreatePillButton("Today", DateRangePreset.Today);
            _btn7Days = CreatePillButton("7 Days", DateRangePreset.SevenDays);
            _btn1Month = CreatePillButton("1 Month", DateRangePreset.OneMonth);
            _btnCustom = CreatePillButton("Custom", DateRangePreset.Custom);

            _customPickerPanel = new Panel
            {
                AutoSize = true,
                Height = 30,
                Visible = false,
                Margin = new Padding(6, 2, 0, 0),
                BackColor = Color.Transparent,
            };

            _dtpStart = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Font = BrandPalette.BrandFontRegular(8.5f),
                Width = 95,
                Value = DateTime.Today.AddMonths(-1),
            };
            _dtpStart.ValueChanged += (s, e) => TriggerCustomRange();

            var toLabel = new Label
            {
                Text = "to",
                AutoSize = true,
                Font = BrandPalette.BrandFontRegular(8.5f),
                ForeColor = BrandPalette.NeutralTextMuted,
                Location = new Point(100, 6),
            };

            _dtpEnd = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Font = BrandPalette.BrandFontRegular(8.5f),
                Width = 95,
                Location = new Point(120, 0),
                Value = DateTime.Today,
            };
            _dtpEnd.ValueChanged += (s, e) => TriggerCustomRange();

            _customPickerPanel.Controls.Add(_dtpStart);
            _customPickerPanel.Controls.Add(toLabel);
            _customPickerPanel.Controls.Add(_dtpEnd);

            flow.Controls.Add(label);
            flow.Controls.Add(_btnToday);
            flow.Controls.Add(_btn7Days);
            flow.Controls.Add(_btn1Month);
            flow.Controls.Add(_btnCustom);
            flow.Controls.Add(_customPickerPanel);

            Controls.Add(flow);

            // Default to 1 Month
            SelectPreset(DateRangePreset.OneMonth, notify: false);
        }

        private Button CreatePillButton(string text, DateRangePreset preset)
        {
            var btn = new Button
            {
                Text = text,
                Height = 30,
                Width = 72,
                FlatStyle = FlatStyle.Flat,
                Font = BrandPalette.BrandFontRegular(8.5f),
                Cursor = Cursors.Hand,
                Margin = new Padding(2, 2, 2, 2),
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += (s, e) => SelectPreset(preset, notify: true);
            return btn;
        }

        public void SelectPreset(DateRangePreset preset, bool notify = true)
        {
            SelectedPreset = preset;

            var today = DateTime.Today;

            switch (preset)
            {
                case DateRangePreset.Today:
                    StartDate = today;
                    EndDate = today.AddDays(1).AddTicks(-1);
                    _customPickerPanel.Visible = false;
                    break;
                case DateRangePreset.SevenDays:
                    StartDate = today.AddDays(-6);
                    EndDate = today.AddDays(1).AddTicks(-1);
                    _customPickerPanel.Visible = false;
                    break;
                case DateRangePreset.OneMonth:
                    StartDate = today.AddMonths(-1);
                    EndDate = today.AddDays(1).AddTicks(-1);
                    _customPickerPanel.Visible = false;
                    break;
                case DateRangePreset.Custom:
                    StartDate = _dtpStart.Value.Date;
                    EndDate = _dtpEnd.Value.Date.AddDays(1).AddTicks(-1);
                    _customPickerPanel.Visible = true;
                    break;
            }

            UpdateButtonStyles();

            if (notify)
            {
                RangeChanged?.Invoke(this, new DashboardDateRangeEventArgs(SelectedPreset, StartDate, EndDate));
            }
        }

        private void TriggerCustomRange()
        {
            if (SelectedPreset != DateRangePreset.Custom) return;

            StartDate = _dtpStart.Value.Date;
            EndDate = _dtpEnd.Value.Date.AddDays(1).AddTicks(-1);
            RangeChanged?.Invoke(this, new DashboardDateRangeEventArgs(SelectedPreset, StartDate, EndDate));
        }

        private void UpdateButtonStyles()
        {
            StylePill(_btnToday, SelectedPreset == DateRangePreset.Today);
            StylePill(_btn7Days, SelectedPreset == DateRangePreset.SevenDays);
            StylePill(_btn1Month, SelectedPreset == DateRangePreset.OneMonth);
            StylePill(_btnCustom, SelectedPreset == DateRangePreset.Custom);
        }

        private static void StylePill(Button btn, bool isActive)
        {
            if (isActive)
            {
                btn.BackColor = BrandPalette.ButtonPrimaryFill;
                btn.ForeColor = BrandPalette.ButtonPrimaryText;
                btn.Font = BrandPalette.BrandFontBold(8.5f);
            }
            else
            {
                btn.BackColor = BrandPalette.ButtonSecondaryFill;
                btn.ForeColor = BrandPalette.ButtonSecondaryText;
                btn.Font = BrandPalette.BrandFontRegular(8.5f);
            }
        }
    }
}

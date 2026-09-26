using CRM_Morcaffein.Theme;

namespace CRM_Morcaffein.winforms.Views.Controls
{
    /// <summary>
    /// Which report to generate when the user clicks Export PDF. Kept as a small
    /// enum so callers can branch on it without string comparison.
    /// </summary>
    public enum ReportKind
    {
        Summary,
        TransactionLedger,
    }

    /// <summary>
    /// Small modal that asks the user which report to export. Returns OK with
    /// SelectedReport set; returns Cancel otherwise. No file I/O here — the
    /// caller runs the SaveFileDialog and generation flow after this closes.
    ///
    /// The labels for each option differ by role, so the modal accepts a
    /// summaryDescription parameter that the caller supplies — Manager's summary
    /// is operations-focused, Admin's is business-focused.
    /// </summary>
    public class ExportReportTypeModal : Form
    {
        public ReportKind SelectedReport { get; private set; } = ReportKind.Summary;

        private readonly RadioButton _summaryRadio;
        private readonly RadioButton _ledgerRadio;

        public ExportReportTypeModal(string summaryDescription)
        {
            Text = "Export Report";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(420, 260);
            BackColor = BrandPalette.SurfacePrimary;
            Font = BrandPalette.BrandFontRegular(9.5f);
            Padding = new Padding(24);

            // ---- Title ----
            var titleLabel = new Label
            {
                Text = "Export Report",
                Location = new Point(24, 20),
                AutoSize = true,
                Font = BrandPalette.BrandFontBold(13f),
                ForeColor = BrandPalette.NeutralText,
                BackColor = Color.Transparent,
            };

            var subtitleLabel = new Label
            {
                Text = "Choose which report to generate.",
                Location = new Point(24, 46),
                AutoSize = true,
                Font = BrandPalette.BrandFontRegular(9f),
                ForeColor = BrandPalette.NeutralTextMuted,
                BackColor = Color.Transparent,
            };

            // ---- Option 1: Summary ----
            _summaryRadio = new RadioButton
            {
                Text = "Summary Report",
                Location = new Point(28, 90),
                AutoSize = true,
                Checked = true,
                Font = BrandPalette.BrandFontBold(10f),
                ForeColor = BrandPalette.NeutralText,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
            };

            var summaryDesc = new Label
            {
                Text = summaryDescription,
                Location = new Point(48, 112),
                Size = new Size(340, 32),
                Font = BrandPalette.BrandFontRegular(8.5f),
                ForeColor = BrandPalette.NeutralTextMuted,
                BackColor = Color.Transparent,
            };

            // ---- Option 2: Transactions Ledger ----
            _ledgerRadio = new RadioButton
            {
                Text = "Transactions Ledger",
                Location = new Point(28, 154),
                AutoSize = true,
                Font = BrandPalette.BrandFontBold(10f),
                ForeColor = BrandPalette.NeutralText,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
            };

            var ledgerDesc = new Label
            {
                Text = "Every completed order in the selected period.",
                Location = new Point(48, 176),
                Size = new Size(340, 20),
                Font = BrandPalette.BrandFontRegular(8.5f),
                ForeColor = BrandPalette.NeutralTextMuted,
                BackColor = Color.Transparent,
            };

            // ---- Buttons ----
            var cancelBtn = new Button
            {
                Text = "Cancel",
                Size = new Size(100, 34),
                Location = new Point(ClientSize.Width - 24 - 100, 210),
                FlatStyle = FlatStyle.Flat,
                BackColor = BrandPalette.ButtonSecondaryFill,
                ForeColor = BrandPalette.ButtonSecondaryText,
                Font = BrandPalette.BrandFontRegular(9.5f),
                Cursor = Cursors.Hand,
            };
            cancelBtn.FlatAppearance.BorderSize = 0;
            cancelBtn.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            var exportBtn = new Button
            {
                Text = "Export",
                Size = new Size(100, 34),
                Location = new Point(ClientSize.Width - 24 - 100 - 8 - 100, 210),
                FlatStyle = FlatStyle.Flat,
                BackColor = BrandPalette.ButtonPrimaryFill,
                ForeColor = BrandPalette.ButtonPrimaryText,
                Font = BrandPalette.BrandFontBold(9.5f),
                Cursor = Cursors.Hand,
            };
            exportBtn.FlatAppearance.BorderSize = 0;
            exportBtn.Click += (s, e) =>
            {
                SelectedReport = _ledgerRadio.Checked
                    ? ReportKind.TransactionLedger
                    : ReportKind.Summary;

                DialogResult = DialogResult.OK;
                Close();
            };

            Controls.Add(titleLabel);
            Controls.Add(subtitleLabel);
            Controls.Add(_summaryRadio);
            Controls.Add(summaryDesc);
            Controls.Add(_ledgerRadio);
            Controls.Add(ledgerDesc);
            Controls.Add(exportBtn);
            Controls.Add(cancelBtn);

            AcceptButton = exportBtn;
            CancelButton = cancelBtn;
        }
    }
}
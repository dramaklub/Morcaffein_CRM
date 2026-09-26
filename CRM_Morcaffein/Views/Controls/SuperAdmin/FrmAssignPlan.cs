using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.Theme;
using CRM_Morcaffein.winforms.Services.Api;

namespace CRM_Morcaffein.winforms.Views.Controls
{
    /// <summary>
    /// Sub-modal for assigning a subscription plan to a company.
    /// Opened by FrmCompanyDetail's "Change Plan" button. Shows the three
    /// plans as radio options with their modules and prices, plus an
    /// "Agreed by" text field for the T&C record.
    ///
    /// On success, returns DialogResult.OK and the caller (FrmCompanyDetail)
    /// refreshes its subscription history in place.
    /// </summary>
    public class FrmAssignPlan : Form
    {
        private readonly SuperAdminApi _api;
        private readonly int _companyId;
        private readonly string _companyName;
        private readonly List<SuperAdminPlanDto> _plans;

        private readonly List<RadioButton> _planRadios = new();
        private TextBox _agreedByTxtBx = null!;
        private Label _errorLabel = null!;
        private Button _assignBtn = null!;

        public FrmAssignPlan(
            SuperAdminApi api,
            int companyId,
            string companyName,
            List<SuperAdminPlanDto> plans)
        {
            _api = api;
            _companyId = companyId;
            _companyName = companyName;
            _plans = plans;

            Text = $"Assign Plan to {companyName}";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(560, 520);
            BackColor = BrandPalette.SurfaceCard;
            Font = BrandPalette.BrandFontRegular(9.5f);
            Padding = new Padding(24);

            BuildLayout();
        }

        private void BuildLayout()
        {
            // ---- Title ----
            var titleLabel = new Label
            {
                Text = "Assign Plan",
                Location = new Point(0, 0),
                AutoSize = true,
                Font = BrandPalette.BrandFontBold(14f),
                ForeColor = BrandPalette.NeutralText,
                BackColor = Color.Transparent,
            };

            var subtitleLabel = new Label
            {
                Text = $"Choose a subscription plan for {_companyName}.",
                Location = new Point(0, 26),
                AutoSize = true,
                Font = BrandPalette.BrandFontRegular(9f),
                ForeColor = BrandPalette.NeutralTextMuted,
                BackColor = Color.Transparent,
            };

            // ---- Plan options ----
            var planHost = new FlowLayoutPanel
            {
                Location = new Point(0, 60),
                Width = 512,
                Height = 340,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Color.Transparent,
                AutoScroll = false,
            };

            bool first = true;
            foreach (var plan in _plans)
            {
                var option = BuildPlanOption(plan, checkedByDefault: first);
                planHost.Controls.Add(option);
                first = false;
            }

            // ---- Agreed by ----
            var agreedByLabel = new Label
            {
                Text = "Agreed by *",
                Location = new Point(0, 412),
                AutoSize = true,
                Font = BrandPalette.BrandFontRegular(9f),
                ForeColor = BrandPalette.NeutralTextMuted,
                BackColor = Color.Transparent,
            };

            _agreedByTxtBx = new TextBox
            {
                Location = new Point(0, 432),
                Width = 512,
                Height = 26,
                Font = BrandPalette.BrandFontRegular(9.5f),
                BorderStyle = BorderStyle.FixedSingle,
                Text = ApiConfig.SuperAdminName ?? "Platform Operator",
            };

            // ---- Error label ----
            _errorLabel = new Label
            {
                Location = new Point(0, 464),
                Size = new Size(512, 20),
                Font = BrandPalette.BrandFontRegular(8.5f),
                ForeColor = BrandPalette.StatusNegativeFill,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent,
            };

            // ---- Buttons ----
            var cancelBtn = new Button
            {
                Text = "Cancel",
                Size = new Size(120, 34),
                Location = new Point(ClientSize.Width - 24 - 120, ClientSize.Height - 24 - 34),
                FlatStyle = FlatStyle.Flat,
                BackColor = BrandPalette.ButtonSecondaryFill,
                ForeColor = BrandPalette.ButtonSecondaryText,
                Font = BrandPalette.BrandFontRegular(9.5f),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            };
            cancelBtn.FlatAppearance.BorderSize = 0;
            cancelBtn.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            _assignBtn = new Button
            {
                Text = "Assign Plan",
                Size = new Size(140, 34),
                Location = new Point(ClientSize.Width - 24 - 120 - 8 - 140, ClientSize.Height - 24 - 34),
                FlatStyle = FlatStyle.Flat,
                BackColor = BrandPalette.ButtonPrimaryFill,
                ForeColor = BrandPalette.ButtonPrimaryText,
                Font = BrandPalette.BrandFontBold(9.5f),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            };
            _assignBtn.FlatAppearance.BorderSize = 0;
            _assignBtn.Click += AssignBtn_Click;

            Controls.Add(titleLabel);
            Controls.Add(subtitleLabel);
            Controls.Add(planHost);
            Controls.Add(agreedByLabel);
            Controls.Add(_agreedByTxtBx);
            Controls.Add(_errorLabel);
            Controls.Add(_assignBtn);
            Controls.Add(cancelBtn);

            AcceptButton = _assignBtn;
            CancelButton = cancelBtn;
        }

        /// <summary>
        /// One plan option — a bordered panel with a radio button on the left,
        /// plan name and price on top, module list below.
        /// </summary>
        private Panel BuildPlanOption(SuperAdminPlanDto plan, bool checkedByDefault)
        {
            var option = new Panel
            {
                Width = 500,
                Height = 96,
                Margin = new Padding(0, 0, 0, 10),
                BackColor = BrandPalette.SurfacePrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Cursor = Cursors.Hand,
            };

            var radio = new RadioButton
            {
                Location = new Point(14, 14),
                Size = new Size(20, 20),
                Checked = checkedByDefault,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
            };
            _planRadios.Add(radio);

            var nameLabel = new Label
            {
                Text = plan.Name,
                Location = new Point(42, 14),
                AutoSize = true,
                Font = BrandPalette.BrandFontBold(12f),
                ForeColor = BrandPalette.Brand,
                BackColor = Color.Transparent,
            };

            var priceLabel = new Label
            {
                Text = $"₱{plan.MonthlyPrice:N0} / month",
                Location = new Point(380, 16),
                Size = new Size(110, 20),
                Font = BrandPalette.BrandFontBold(10f),
                ForeColor = BrandPalette.NeutralText,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleRight,
            };

            var modulesLabel = new Label
            {
                Text = HumanizeModules(plan.ModulesCsv),
                Location = new Point(42, 40),
                Size = new Size(440, 44),
                Font = BrandPalette.BrandFontRegular(9f),
                ForeColor = BrandPalette.NeutralTextMuted,
                BackColor = Color.Transparent,
            };

            // Clicking anywhere on the panel toggles the radio.
            void Toggle(object? sender, EventArgs e)
            {
                radio.Checked = true;
            }
            option.Click += Toggle;
            nameLabel.Click += Toggle;
            modulesLabel.Click += Toggle;
            priceLabel.Click += Toggle;

            option.Controls.Add(radio);
            option.Controls.Add(nameLabel);
            option.Controls.Add(priceLabel);
            option.Controls.Add(modulesLabel);
            return option;
        }

        private static string HumanizeModules(string csv)
        {
            if (string.IsNullOrWhiteSpace(csv)) return "—";

            var names = csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(key => key switch
                {
                    "DataCollection" => "Data Collection",
                    "Transactions" => "Transactions",
                    "Feedback" => "Feedback",
                    "Retention" => "Retention",
                    "BI" => "Business Intelligence",
                    "BranchManagement" => "Branch Management",
                    _ => key,
                });

            return string.Join("  ·  ", names);
        }

        private async void AssignBtn_Click(object? sender, EventArgs e)
        {
            _errorLabel.Text = string.Empty;

            // Which plan is selected?
            int selectedIndex = _planRadios.FindIndex(r => r.Checked);
            if (selectedIndex < 0)
            {
                _errorLabel.Text = "Please choose a plan.";
                return;
            }

            var selectedPlan = _plans[selectedIndex];

            var agreedBy = _agreedByTxtBx.Text.Trim();
            if (string.IsNullOrWhiteSpace(agreedBy))
            {
                _errorLabel.Text = "Who accepted the T&C is required.";
                return;
            }

            try
            {
                _assignBtn.Enabled = false;
                _assignBtn.Text = "Assigning…";

                await _api.AssignPlanAsync(_companyId, selectedPlan.PlanId, agreedBy);

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                _errorLabel.Text = ex.Message;
            }
            finally
            {
                _assignBtn.Enabled = true;
                _assignBtn.Text = "Assign Plan";
            }
        }
    }
}
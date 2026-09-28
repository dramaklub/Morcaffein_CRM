using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.Theme;
using CRM_Morcaffein.winforms.Services;

namespace CRM_Morcaffein.winforms.Views
{
    /// <summary>
    /// Modal for creating a new branch or editing an existing one. Two modes:
    ///   Create — empty fields, "Create Branch" button
    ///   Edit   — prefilled from an existing BranchResponseDto, "Save Changes" button
    ///
    /// Same layout pattern as FrmCreateEmployee: single absolute-positioned panel,
    /// PAD = 40 left inset, SurfaceCard background, bottom-docked button row.
    ///
    /// Branch name is required (max 100 chars). Address is optional (max 250 chars).
    /// Duplicate-name checks happen server-side; the client does no pre-flight,
    /// just surfaces whatever 409 comes back.
    /// </summary>
    public class FrmBranchDetail : Form
    {
        private enum Mode { Create, Edit }

        private readonly BranchService _service;
        private readonly Mode _mode;
        private readonly BranchResponseDto? _existing;

        private TextBox _nameTxtBx = null!;
        private TextBox _addressTxtBx = null!;
        private Label _errorLabel = null!;
        private Button _primaryBtn = null!;

        private const int FormWidth = 520;
        private const int PAD = 40;
        private const int FieldW = 392;

        /// <summary>The branch that was created or updated. Populated on success.</summary>
        public BranchResponseDto? Result { get; private set; }

        private FrmBranchDetail(BranchService service, Mode mode, BranchResponseDto? existing)
        {
            _service = service;
            _mode = mode;
            _existing = existing;

            Text = mode == Mode.Create ? "New Branch" : "Edit Branch";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(FormWidth, 400);
            BackColor = BrandPalette.SurfaceCard;
            Font = BrandPalette.BrandFontRegular(9.5f);
            Padding = new Padding(24);

            BuildLayout();

            if (_mode == Mode.Edit && _existing is not null)
                PopulateFromExisting();
        }

        /// <summary>Creates a modal in "New Branch" mode.</summary>
        public static FrmBranchDetail ForCreate(BranchService service)
            => new FrmBranchDetail(service, Mode.Create, null);

        /// <summary>Creates a modal in "Edit Branch" mode, prefilled.</summary>
        public static FrmBranchDetail ForEdit(BranchService service, BranchResponseDto branch)
            => new FrmBranchDetail(service, Mode.Edit, branch);

        private void BuildLayout()
        {
            var titleLabel = new Label
            {
                Text = _mode == Mode.Create ? "Create Branch" : "Edit Branch",
                Location = new Point(0, 0),
                AutoSize = true,
                Font = BrandPalette.BrandFontBold(14f),
                ForeColor = BrandPalette.NeutralText,
                BackColor = Color.Transparent,
            };

            var subtitleLabel = new Label
            {
                Text = _mode == Mode.Create
                    ? "Add a new physical location for this business."
                    : "Update the branch name or address.",
                Location = new Point(0, 26),
                AutoSize = true,
                Font = BrandPalette.BrandFontRegular(9f),
                ForeColor = BrandPalette.NeutralTextMuted,
                BackColor = Color.Transparent,
            };

            // ---- Form host: absolute-positioned fields ----
            var formHost = new Panel
            {
                Location = new Point(0, 66),
                Size = new Size(FormWidth - 48, 220),
                BackColor = BrandPalette.SurfaceCard,
            };

            const int labelH = 18;
            const int fieldH = 30;
            const int rowH = 72;
            int y = 0;

            // ---- Branch name ----
            formHost.Controls.Add(new Label
            {
                Text = "Branch name * (max 100 chars)",
                Location = new Point(PAD, y),
                AutoSize = true,
                Font = BrandPalette.BrandFontRegular(9f),
                ForeColor = BrandPalette.NeutralTextMuted,
                BackColor = Color.Transparent,
            });
            _nameTxtBx = new TextBox
            {
                Location = new Point(PAD, y + labelH),
                Size = new Size(FieldW, fieldH),
                Font = BrandPalette.BrandFontRegular(9.5f),
                BorderStyle = BorderStyle.FixedSingle,
                MaxLength = 100,
            };
            formHost.Controls.Add(_nameTxtBx);
            y += rowH;

            // ---- Address ----
            formHost.Controls.Add(new Label
            {
                Text = "Address (optional, max 250 chars)",
                Location = new Point(PAD, y),
                AutoSize = true,
                Font = BrandPalette.BrandFontRegular(9f),
                ForeColor = BrandPalette.NeutralTextMuted,
                BackColor = Color.Transparent,
            });
            _addressTxtBx = new TextBox
            {
                Location = new Point(PAD, y + labelH),
                Size = new Size(FieldW, 60),
                Multiline = true,
                WordWrap = true,
                ScrollBars = ScrollBars.Vertical,
                Font = BrandPalette.BrandFontRegular(9.5f),
                BorderStyle = BorderStyle.FixedSingle,
                MaxLength = 250,
                PlaceholderText = "Street, building, area (optional)",
            };
            formHost.Controls.Add(_addressTxtBx);
            y += 90;

            // ---- Error label ----
            _errorLabel = new Label
            {
                Location = new Point(PAD, y + 4),
                Size = new Size(FieldW, 26),
                Font = BrandPalette.BrandFontRegular(8.5f),
                ForeColor = BrandPalette.StatusNegativeFill,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent,
            };
            formHost.Controls.Add(_errorLabel);

            // ---- Bottom button row ----
            var btnRow = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 56,
                BackColor = Color.Transparent,
            };

            var cancelBtn = new Button
            {
                Text = "Cancel",
                Size = new Size(120, 36),
                Location = new Point(btnRow.Width - 120, 10),
                FlatStyle = FlatStyle.Flat,
                BackColor = BrandPalette.ButtonSecondaryFill,
                ForeColor = BrandPalette.ButtonSecondaryText,
                Font = BrandPalette.BrandFontRegular(9.5f),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
            };
            cancelBtn.FlatAppearance.BorderSize = 0;
            cancelBtn.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            _primaryBtn = new Button
            {
                Text = _mode == Mode.Create ? "Create Branch" : "Save Changes",
                Size = new Size(160, 36),
                Location = new Point(btnRow.Width - 120 - 8 - 160, 10),
                FlatStyle = FlatStyle.Flat,
                BackColor = BrandPalette.ButtonPrimaryFill,
                ForeColor = BrandPalette.ButtonPrimaryText,
                Font = BrandPalette.BrandFontBold(9.5f),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
            };
            _primaryBtn.FlatAppearance.BorderSize = 0;
            _primaryBtn.Click += PrimaryBtn_Click;

            btnRow.Controls.Add(_primaryBtn);
            btnRow.Controls.Add(cancelBtn);

            // formHost fills the space above the button row.
            formHost.Size = new Size(FormWidth - 48, ClientSize.Height - 66 - 56 - 24);

            Controls.Add(formHost);
            Controls.Add(titleLabel);
            Controls.Add(subtitleLabel);
            Controls.Add(btnRow);

            _nameTxtBx.TabIndex = 0;
            _addressTxtBx.TabIndex = 1;
            _primaryBtn.TabIndex = 2;
            cancelBtn.TabIndex = 3;

            AcceptButton = _primaryBtn;
            CancelButton = cancelBtn;
        }

        private void PopulateFromExisting()
        {
            if (_existing is null) return;

            _nameTxtBx.Text = _existing.BranchName;
            _addressTxtBx.Text = _existing.Address ?? string.Empty;

            // Focus the name field with cursor at end.
            _nameTxtBx.SelectionStart = _nameTxtBx.Text.Length;
            _nameTxtBx.Focus();
        }

        private async void PrimaryBtn_Click(object? sender, EventArgs e)
        {
            _errorLabel.Text = string.Empty;

            var name = _nameTxtBx.Text.Trim();
            var address = _addressTxtBx.Text.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                _errorLabel.Text = "Branch name is required.";
                return;
            }

            if (name.Length > 100)
            {
                _errorLabel.Text = "Branch name cannot exceed 100 characters.";
                return;
            }

            if (address.Length > 250)
            {
                _errorLabel.Text = "Address cannot exceed 250 characters.";
                return;
            }

            try
            {
                _primaryBtn.Enabled = false;
                _primaryBtn.Text = _mode == Mode.Create ? "Creating…" : "Saving…";

                Result = _mode == Mode.Create
                    ? await _service.CreateAsync(name, address)
                    : await _service.UpdateAsync(_existing!.BranchId, name, address);

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                _errorLabel.Text = ex.Message;
            }
            finally
            {
                _primaryBtn.Enabled = true;
                _primaryBtn.Text = _mode == Mode.Create ? "Create Branch" : "Save Changes";
            }
        }
    }
}
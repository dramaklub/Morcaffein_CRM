using System.Windows.Forms.DataVisualization.Charting;
using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.Theme;

namespace CRM_Morcaffein.winforms.Views.Controls
{
    /// <summary>Campaign ROI — grouped column: Revenue driven vs. Discount cost per campaign.</summary>
    public class CampaignRoiControl : Panel
    {
        private readonly Chart _chart;
        private readonly Label _titleLabel;
        private readonly Label _statusLabel;

        public CampaignRoiControl()
        {
            Height      = 280;
            MinimumSize = new Size(100, 100);
            Margin      = new Padding(0, 0, 0, 10);
            BackColor   = BrandPalette.SurfacePrimary;

            _titleLabel = new Label
            {
                Text      = "Campaign ROI",
                Dock      = DockStyle.Top,
                Height    = 32,
                Padding   = new Padding(14, 8, 0, 0),
                Font      = BrandPalette.BrandFontBold(10.5f),
                ForeColor = BrandPalette.NeutralText,
                BackColor = Color.Transparent,
            };

            var divider = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = BrandPalette.Border };

            _statusLabel = new Label
            {
                Text      = "No campaign data yet.",
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font      = BrandPalette.BrandFontRegular(9.5f),
                ForeColor = BrandPalette.NeutralTextMuted,
            };

            _chart = new Chart
            {
                Dock            = DockStyle.Fill,
                MinimumSize     = new Size(50, 50),
                BackColor       = BrandPalette.SurfacePrimary,
                BorderlineColor = Color.Transparent,
                BorderlineWidth = 0,
                Visible         = false,
            };

            var area = new ChartArea("MainArea")
            {
                BackColor   = BrandPalette.SurfacePrimary,
                BorderColor = Color.Transparent,
                BorderWidth = 0,
            };
            area.AxisX.MajorGrid.Enabled   = false;
            area.AxisY.MajorGrid.LineColor  = BrandPalette.Border;
            area.AxisX.Interval             = 1;
            area.AxisX.IsMarginVisible      = true;
            area.AxisX.LabelStyle.Font      = BrandPalette.BrandFontRegular(8f);
            area.AxisY.LabelStyle.Font      = BrandPalette.BrandFontRegular(8f);
            area.AxisY.Title                = "Amount ₱";
            area.AxisY.TitleFont            = BrandPalette.BrandFontBold(8f);

            _chart.ChartAreas.Add(area);

            var legend = new Legend("L")
            {
                Docking                 = Docking.Bottom,
                Alignment               = StringAlignment.Center,
                BackColor               = Color.Transparent,
                Font                    = BrandPalette.BrandFontRegular(8f),
                IsDockedInsideChartArea = false,
            };
            _chart.Legends.Add(legend);

            Controls.Add(_chart);
            Controls.Add(_statusLabel);
            Controls.Add(divider);
            Controls.Add(_titleLabel);
        }

        public void BindData(CampaignRoiDto data)
        {
            if (data.Campaigns.Count == 0 || data.Campaigns.All(c => c.TotalRedemptions == 0))
            {
                _statusLabel.Text    = "No campaign activity yet.";
                _statusLabel.Visible = true;
                _chart.Visible       = false;
                return;
            }

            _statusLabel.Visible = false;
            _chart.Visible       = true;
            _chart.Series.Clear();

            var seriesRev = new Series("Revenue Driven")
            {
                ChartType   = SeriesChartType.Column,
                ChartArea   = "MainArea",
                Legend      = "L",
                Color       = BrandPalette.Brand,
                BorderWidth = 0,
            };
            seriesRev["PointWidth"] = "0.5";

            var seriesDisc = new Series("Discount Cost")
            {
                ChartType   = SeriesChartType.Column,
                ChartArea   = "MainArea",
                Legend      = "L",
                Color       = BrandPalette.Tint50,
                BorderWidth = 0,
            };
            seriesDisc["PointWidth"] = "0.5";

            foreach (var c in data.Campaigns)
            {
                seriesRev.Points.AddXY(c.PromoName, (double)c.RevenueGenerated);
                seriesDisc.Points.AddXY(c.PromoName, (double)c.TotalDiscountCost);
            }

            _chart.Series.Add(seriesRev);
            _chart.Series.Add(seriesDisc);
            _chart.Invalidate();
        }
    }
}
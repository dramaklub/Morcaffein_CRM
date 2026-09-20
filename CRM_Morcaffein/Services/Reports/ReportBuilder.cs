using CRM_Morcaffein.api.DTOs;

namespace CRM_Morcaffein.winforms.Services.Reports
{
    /// <summary>
    /// DTOs → ReportDocument. Knows the domain; knows nothing about PDF.
    /// </summary>
    public static class ReportBuilder
    {
        public static ReportDocument BuildManager(
            int days,
            PosDiagnosticsDto pos,
            PeakHoursDto peak,
            AgeDemographicsAovDto age,
            TopProductsResponseDto topProducts,
            ActiveCampaignLiftDto lift,
            FeedbackRatingDistributionDto csat,
            LoyaltyTierProgressionDto tier)
        {
            var doc = new ReportDocument
            {
                Title = "Morcaffein — Operations Report",
                Subtitle = "Store Manager summary",
                PeriodLabel = PeriodLabelFor(days),
                GeneratedBy = Session.EmployeeSession.Current?.DisplayName ?? "—",
                GeneratedAt = DateTime.Now,
            };

            doc.Sections.Add(BuildKpiSection(pos, csat));
            doc.Sections.Add(BuildPeakHoursSection(peak));
            doc.Sections.Add(BuildAgeDemographicsSection(age));
            doc.Sections.Add(BuildTopProductsSection(topProducts));
            doc.Sections.Add(BuildCampaignLiftSection(lift));
            doc.Sections.Add(BuildRatingsSection(csat));

            // Member Growth by Tier goes last — the report's operational content
            // comes first, this trend sits at the bottom as a reference.
            doc.Sections.Add(BuildLoyaltyTierSection(tier));

            return doc;
        }

        public static ReportDocument BuildAdmin(
    int days,
    PosDiagnosticsDto pos,
    CustomerLtvChurnDto ltv,
    RevenueVsDiscountsDto revDisc,
    NewVsReturningDto newRet,
    PeakHoursDto peak,
    AuditLogSummaryDto audit)
        {
            var doc = new ReportDocument
            {
                Title = "Morcaffein — Business Report",
                Subtitle = "Branch Admin summary",
                PeriodLabel = PeriodLabelFor(days),
                GeneratedBy = Session.EmployeeSession.Current?.DisplayName ?? "—",
                GeneratedAt = DateTime.Now,
            };

            doc.Sections.Add(new ReportSection
            {
                Heading = "Key Metrics",
                Blocks =
                {
                    new MetricStripBlock
                    {
                        Metrics =
                        {
                            ("Total Revenue",         $"₱{pos.NetSales:N0}"),
                            ("Member Lifetime Value", $"₱{ltv.OverallAverageLtv:N0}"),
                            ("Pending Approvals",     $"{audit.PendingCampaignApprovals:N0}"),
                        }
                    }
                }
            });

            doc.Sections.Add(BuildRevenueVsDiscountsSection(revDisc));
            doc.Sections.Add(BuildLtvChurnSection(ltv));
            doc.Sections.Add(BuildNewVsReturningSection(newRet));
            doc.Sections.Add(BuildPeakHoursSection(peak));
            doc.Sections.Add(BuildAuditSection(audit));

            return doc;
        }

        // ---- Manager sections ----

        private static ReportSection BuildKpiSection(PosDiagnosticsDto pos, FeedbackRatingDistributionDto csat)
        {
            var aov = pos.TotalTransactions > 0
                ? pos.NetSales / pos.TotalTransactions
                : 0m;

            return new ReportSection
            {
                Heading = "Key Metrics",
                Blocks =
                {
                    new MetricStripBlock
                    {
                        Metrics =
                        {
                            ("Total Revenue",  $"₱{pos.NetSales:N0}"),
                            ("Avg. per Sale",  $"₱{aov:N0}"),
                            ("Promos Used",    $"{pos.CampaignTransactionCount:N0}"),
                            ("Avg. Rating", csat.TotalFeedbacks > 0 ? $"{csat.AverageRating:0.0} / 5" : "—"),
                        }
                    }
                }
            };
        }

        private static ReportSection BuildPeakHoursSection(PeakHoursDto peak)
        {
            var section = new ReportSection
            {
                Heading = "Peak Hours",
                Subtitle = "Orders by hour and day of week",
            };

            if (peak.Cells.Count == 0)
            {
                section.Blocks.Add(new ParagraphBlock { Text = "No orders recorded." });
                return section;
            }

            var days = new[] { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };
            var sorted = peak.Cells.OrderByDescending(c => c.TransactionCount).Take(15).ToList();

            var table = new TableBlock { Columns = { "Day", "Hour", "Orders", "Revenue" } };
            foreach (var c in sorted)
            {
                var hour = c.Hour.ToString("00");
                table.Rows.Add(new[]
                {
                    days[Math.Clamp(c.DayOfWeek, 0, 6)],
                    $"{hour}:00",
                    c.TransactionCount.ToString("N0"),
                    $"₱{c.Revenue:N0}",
                });
            }

            section.Blocks.Add(new ParagraphBlock
            {
                Text = "Top 15 busiest cells (hour × day), sorted by order count.",
                Muted = true,
            });
            section.Blocks.Add(table);
            return section;
        }

        private static ReportSection BuildAgeDemographicsSection(AgeDemographicsAovDto age)
        {
            var section = new ReportSection
            {
                Heading = "Who's Shopping With Us",
                Subtitle = "Members by age bracket and average spend",
            };

            var table = new TableBlock { Columns = { "Age Bracket", "Members", "Share", "Avg. Spend" } };
            foreach (var b in age.Brackets.Where(x =>
                !x.BracketLabel.Equals("Unknown", StringComparison.OrdinalIgnoreCase)))
            {
                table.Rows.Add(new[]
                {
                    b.BracketLabel,
                    b.ActiveMemberCount.ToString("N0"),
                    $"{b.PercentageOfTotal:0}%",
                    b.AverageOrderValue > 0 ? $"₱{b.AverageOrderValue:N0}" : "—",
                });
            }

            section.Blocks.Add(table);
            return section;
        }
        private static ReportSection BuildTopProductsSection(TopProductsResponseDto topProducts)
        {
            var section = new ReportSection
            {
                Heading = "Top Coffees",
                Subtitle = "Best-selling drinks by revenue",
            };

            if (topProducts == null || topProducts.Products.Count == 0)
            {
                section.Blocks.Add(new ParagraphBlock { Text = "No sales in this period." });
                return section;
            }

            var table = new TableBlock { Columns = { "Rank", "Product", "Category", "Units", "Revenue" } };
            for (int i = 0; i < topProducts.Products.Count; i++)
            {
                var p = topProducts.Products[i];
                table.Rows.Add(new[]
                {
                    $"#{i + 1}",
                    p.ProductName,
                    p.ProductCategory,
                    p.UnitsSold.ToString("N0"),
                    $"₱{p.Revenue:N0}",
                });
            }

            section.Blocks.Add(table);
            return section;
        }
        private static ReportSection BuildLoyaltyTierSection(LoyaltyTierProgressionDto tier)
        {
            var section = new ReportSection
            {
                Heading = "Member Growth by Tier",
                Subtitle = "Month-over-month member counts per tier",
            };

            var table = new TableBlock { Columns = { "Month", "Regular", "Silver", "Gold" } };
            foreach (var m in tier.Months)
            {
                table.Rows.Add(new[]
                {
                    m.MonthLabel,
                    m.RegularCount.ToString("N0"),
                    m.SilverCount.ToString("N0"),
                    m.GoldCount.ToString("N0"),
                });
            }

            section.Blocks.Add(table);
            return section;
        }

        private static ReportSection BuildCampaignLiftSection(ActiveCampaignLiftDto lift)
        {
            var section = new ReportSection
            {
                Heading = "Sales & Promo Usage",
                Subtitle = "Daily revenue vs promo redemptions",
            };

            var rows = lift.DailyPoints
                .Where(p => p.DailyRevenue > 0 || p.DailyRedemptions > 0)
                .ToList();

            if (rows.Count == 0)
            {
                section.Blocks.Add(new ParagraphBlock { Text = "No sales or promo activity in this period." });
                return section;
            }

            var table = new TableBlock { Columns = { "Date", "Revenue", "Redemptions" } };
            foreach (var p in rows)
            {
                table.Rows.Add(new[]
                {
                    p.DateLabel,
                    $"₱{p.DailyRevenue:N0}",
                    p.DailyRedemptions.ToString("N0"),
                });
            }

            section.Blocks.Add(table);
            return section;
        }

        private static ReportSection BuildRatingsSection(FeedbackRatingDistributionDto csat)
        {
            var section = new ReportSection
            {
                Heading = "Customer Ratings",
                Subtitle = csat.TotalFeedbacks > 0
                     ? $"Average {csat.AverageRating:0.0} out of 5 · {csat.TotalFeedbacks:N0} reviews"
                    : "No reviews in this period",
            };

            var table = new TableBlock { Columns = { "Stars", "Count", "Share" } };
            for (int star = 1; star <= 5; star++)
            {
                var pt = csat.RatingBreakdown.FirstOrDefault(r => r.Rating == star);
                var count = pt?.Count ?? 0;
                var pct = pt?.Percentage ?? 0m;

                table.Rows.Add(new[]
                {
                    $"{star} star{(star == 1 ? "" : "s")}",
                    count.ToString("N0"),
                    $"{pct:0}%",
                });
            }

            section.Blocks.Add(table);
            return section;
        }

        // ---- Admin sections ----

        private static ReportSection BuildRevenueVsDiscountsSection(RevenueVsDiscountsDto revDisc)
        {
            var totalNet = revDisc.Buckets.Sum(b => b.NetRevenue);
            var totalDiscount = revDisc.Buckets.Sum(b => b.CampaignDiscount + b.PwdSeniorDiscount);
            var gross = totalNet + totalDiscount;
            var discountPct = gross > 0 ? (totalDiscount / gross) * 100m : 0m;

            var section = new ReportSection
            {
                Heading = "Revenue vs Discounts",
                Subtitle = gross > 0
                    ? $"{discountPct:0}% of revenue went to discounts in this period"
                    : "Net revenue, campaign discounts, PWD/Senior discounts per bucket",
            };

            var table = new TableBlock { Columns = { "Period", "Net Revenue", "Campaign Disc.", "PWD/Senior" } };
            foreach (var b in revDisc.Buckets)
            {
                table.Rows.Add(new[]
                {
                    b.BucketLabel,
                    $"₱{b.NetRevenue:N0}",
                    $"₱{b.CampaignDiscount:N0}",
                    $"₱{b.PwdSeniorDiscount:N0}",
                });
            }

            section.Blocks.Add(table);
            return section;
        }

        private static ReportSection BuildLtvChurnSection(CustomerLtvChurnDto ltv)
        {
            var section = new ReportSection
            {
                Heading = "Member Lifetime Value & At-Risk Members",
                Subtitle = "Per-tier average spend and members inactive for 14+ days",
            };

            var table = new TableBlock { Columns = { "Tier", "Avg. Spend", "Members" } };

            table.Rows.Add(new[]
            {
                "Regular",
                $"₱{ltv.RegularTierAvgLtv:N0}",
                "—",
            });
            table.Rows.Add(new[]
            {
                "Silver",
                $"₱{ltv.SilverTierAvgLtv:N0}",
                "—",
            });
            table.Rows.Add(new[]
            {
                "Gold",
                $"₱{ltv.GoldTierAvgLtv:N0}",
                "—",
            });
            table.Rows.Add(new[]
            {
                "Overall",
                $"₱{ltv.OverallAverageLtv:N0}",
                $"{ltv.ActiveMemberCount:N0}",
            });

            section.Blocks.Add(table);

            // At-risk summary line beneath the table.
            section.Blocks.Add(new ParagraphBlock
            {
                Text = $"{ltv.AtRiskTotalCount:N0} at-risk members (all tiers, 14+ days inactive)  ·  " +
                       $"{ltv.AtRiskHighTierCount:N0} of them are Silver or Gold",
                Muted = true,
            });

            return section;
        }

        private static ReportSection BuildNewVsReturningSection(NewVsReturningDto newRet)
        {
            var totalNew = newRet.Buckets.Sum(b => b.NewCustomerCount);
            var totalReturning = newRet.Buckets.Sum(b => b.ReturningCustomerCount);
            var total = totalNew + totalReturning;
            var retentionPct = total > 0 ? ((decimal)totalReturning / total) * 100m : 0m;

            var section = new ReportSection
            {
                Heading = "New vs Returning Customers",
                Subtitle = total > 0
                    ? $"{total:N0} customer visits  ·  {retentionPct:0}% from returning members"
                    : "Distinct customers per month",
            };

            var table = new TableBlock { Columns = { "Month", "New", "Returning" } };
            foreach (var b in newRet.Buckets)
            {
                table.Rows.Add(new[]
                {
                    b.MonthLabel,
                    b.NewCustomerCount.ToString("N0"),
                    b.ReturningCustomerCount.ToString("N0"),
                });
            }

            section.Blocks.Add(table);
            return section;
        }

        private static ReportSection BuildAuditSection(AuditLogSummaryDto audit)
        {
            return new ReportSection
            {
                Heading = "Audit Log Summary",
                Subtitle = "Staff overrides and governance counters",
                Blocks =
                {
                    new TableBlock
                    {
                        Columns = { "Metric", "Count" },
                        Rows =
                        {
                            new[] { "Point Adjustment Requests", audit.TotalManualPointAdjustments.ToString("N0") },
                            new[] { "Approved Adjustments",      audit.ApprovedPointAdjustments.ToString("N0") },
                            new[] { "Rejected Adjustments",      audit.RejectedPointAdjustments.ToString("N0") },
                            new[] { "CSAT Bonus Points Awarded", audit.FeedbackBonusPointsAwarded.ToString("N0") },
                            new[] { "Pending Campaign Approvals", audit.PendingCampaignApprovals.ToString("N0") },
                        }
                    }
                }
            };
        }

        private static string PeriodLabelFor(int days) => days switch
        {
            1 => "Today",
            7 => "Last 7 days",
            30 => "Last 30 days",
            _ => $"Last {days} days",
        };
    }
}
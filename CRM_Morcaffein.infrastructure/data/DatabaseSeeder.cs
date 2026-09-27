using Bogus;
using CRM_Morcaffein.domain.entities;
using CRM_Morcaffein.infrastructure.data;
using Microsoft.EntityFrameworkCore;
namespace CRM_Morcaffein.infrastructure.Data
{
    /// <summary>
    /// One-shot data generator for dashboard/report demos.
    ///
    /// ADDITIVE — running this twice adds more rows, it doesn't reset. If you want
    /// a fresh start, truncate the tables you want reset and re-run.
    ///
    /// Does NOT touch Products, ProductSizes, ProductModifiers, or existing
    /// Employees. Does add 3 new Baristas so Orders have a wider set of handlers.
    ///
    /// The Order math mirrors OrderController.CompleteOrder exactly:
    ///   SubTotal        = sum(Quantity * (BaseSizePrice + modifier prices))
    ///   TotalDiscount   = campaign discount (if any)
    ///   AfterPromo      = SubTotal - TotalDiscount
    ///   NetOfVat        = AfterPromo / 1.12
    ///   PwdDiscount     = NetOfVat * 0.20  (if HasPwdSeniorDiscount)
    ///   TotalAmount     = NetOfVat - PwdDiscount
    ///   PointsEarned    = (int)(TotalAmount / 10)
    /// </summary>
    public static class DatabaseSeeder
    {
        public static async Task SeedAsync(
             TenantCrmDbContext ctx,
             int customersToCreate = 50,
             int ordersToCreate = 200,
             int daysBack = 90,
             int companyId = 1)
        {
            // Deterministic seed — same data every run for a given tenant, so
            // verification is repeatable. But offset by companyId so each tenant
            // gets a *different* set of fake names, otherwise every tenant ends up
            // with identical usernames (which breaks login routing later).
            Randomizer.Seed = new Random(20260921 + companyId);
            var rnd = new Bogus.Randomizer();

            // Bootstrap the product catalog if this tenant's DB is fresh. This
            // is the same menu across every tenant, so seeding it here means
            // adding a new tenant is just "migrate + seed" — no external SQL
            // file required. Idempotent: does nothing if Products exists.
            await EnsureProductCatalogAsync(ctx);

            // ---------- 0. Branches ----------
            // The migration already created Branch 1 and Branch 2. This is a
            // safety net so the seeder works against a fresh DB where the
            // migration hasn't run — but in practice it'll be a no-op.
            if (!await ctx.Branches.AnyAsync())
            {
                ctx.Branches.AddRange(
                    new Branch { BranchName = "Branch 1", Address = "", IsActive = true },
                    new Branch { BranchName = "Branch 2", Address = "", IsActive = true }
                );
                await ctx.SaveChangesAsync();
            }

            var branchIds = await ctx.Branches
                .AsNoTracking()
                .OrderBy(b => b.BranchId)
                .Select(b => b.BranchId)
                .ToListAsync();

            // ---------- 1. Resolve existing reference data ----------

            var allEmployees = await ctx.Employees.AsNoTracking().ToListAsync();
            var existingBaristas = allEmployees
                .Where(e => e.Role.Equals("Barista", StringComparison.OrdinalIgnoreCase))
                .ToList();

            var products = await ctx.Products.AsNoTracking().ToListAsync();
            var sizes = await ctx.ProductSizes.AsNoTracking().ToListAsync();
            var modifiers = await ctx.ProductModifiers.AsNoTracking().ToListAsync();

            if (products.Count == 0 || sizes.Count == 0)
                throw new InvalidOperationException(
                    "Seeder requires existing Products and ProductSizes. Seed those first.");

            // ---------- 2. Add 3 new Baristas ----------

            var employeeFaker = new Faker<Employee>()
                .RuleFor(e => e.FirstName, f => f.Name.FirstName())
                .RuleFor(e => e.LastName, f => f.Name.LastName())
                .RuleFor(e => e.ContactNumber, f => f.Phone.PhoneNumber("09#########").Replace("-", "").Replace(" ", ""))
                .RuleFor(e => e.Email, (f, e) => f.Internet.Email(e.FirstName, e.LastName).ToLower())
                .RuleFor(e => e.Role, () => "Barista")
                .RuleFor(e => e.BranchId, () => rnd.ArrayElement(branchIds.ToArray()))
                .RuleFor(e => e.IsActive, () => true)
                .RuleFor(e => e.CreatedAt, () => DateTime.UtcNow)
                .RuleFor(e => e.UpdatedAt, () => DateTime.UtcNow);

            var newBaristas = employeeFaker.Generate(3);

            // Every seeded employee needs a matching Credential — otherwise they
            // exist as Employees for reporting (sales, feedback) but can't log in,
            // and can't have their password reset (there's nothing to reset).
            // Username = lowercase firstname + lastname (must be unique). Password
            // is a fixed demo value so we can actually log in as these accounts.
            const string DemoPassword = "password123";

            foreach (var e in newBaristas)
            {
                var usernameBase = (e.FirstName + "." + e.LastName).ToLower().Replace(" ", "");
                var username = usernameBase;
                var suffix = 1;

                // Ensure uniqueness against existing credentials + the ones we're adding
                while (await ctx.Credentials.AnyAsync(c => c.Username == username)
                       || newBaristas.Any(b => b != e && (b.FirstName + "." + b.LastName).ToLower().Replace(" ", "") == username))
                {
                    username = usernameBase + suffix++;
                }

                ctx.Credentials.Add(new Credential
                {
                    Username = username,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(DemoPassword),
                    Employee = e,
                    FailedLoginAttempts = 0,
                    IsLocked = false,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                });
            }

            ctx.Employees.AddRange(newBaristas);
            await ctx.SaveChangesAsync();

            // Every tenant needs at least one Manager and one Admin so the
            // tier-specific views (dashboard, campaign approval, manage users)
            // are testable. Without this, only Baristas exist and you can't
            // demo anything above the POS.
            var managerFaker = new Faker<Employee>()
                .RuleFor(e => e.FirstName, f => f.Name.FirstName())
                .RuleFor(e => e.LastName, f => f.Name.LastName())
                .RuleFor(e => e.ContactNumber, f => f.Phone.PhoneNumber("09#########").Replace("-", "").Replace(" ", ""))
                .RuleFor(e => e.Email, (f, e) => f.Internet.Email(e.FirstName, e.LastName).ToLower())
                .RuleFor(e => e.Role, () => "Manager")
                .RuleFor(e => e.BranchId, () => rnd.ArrayElement(branchIds.ToArray()))
                .RuleFor(e => e.IsActive, () => true)
                .RuleFor(e => e.CreatedAt, () => DateTime.UtcNow)
                .RuleFor(e => e.UpdatedAt, () => DateTime.UtcNow);

            var adminFaker = new Faker<Employee>()
                .RuleFor(e => e.FirstName, f => f.Name.FirstName())
                .RuleFor(e => e.LastName, f => f.Name.LastName())
                .RuleFor(e => e.ContactNumber, f => f.Phone.PhoneNumber("09#########").Replace("-", "").Replace(" ", ""))
                .RuleFor(e => e.Email, (f, e) => f.Internet.Email(e.FirstName, e.LastName).ToLower())
                .RuleFor(e => e.Role, () => "Admin")
                .RuleFor(e => e.BranchId, () => (int?)null)   // Admins see all branches
                .RuleFor(e => e.IsActive, () => true)
                .RuleFor(e => e.CreatedAt, () => DateTime.UtcNow)
                .RuleFor(e => e.UpdatedAt, () => DateTime.UtcNow);

            var manager = managerFaker.Generate();
            var admin = adminFaker.Generate();
            var newManagersAndAdmins = new[] { manager, admin };

            // Give both a credential too. Same "firstname.lastname" pattern and
            // password123 as the baristas (DemoPassword const declared above).
            var leadershipCredentials = new List<Credential>();

            foreach (var e in newManagersAndAdmins)
            {
                var usernameBase = (e.FirstName + "." + e.LastName).ToLower().Replace(" ", "");
                var username = usernameBase;
                var suffix = 1;

                while (await ctx.Credentials.AnyAsync(c => c.Username == username)
                       || leadershipCredentials.Any(c => c.Username == username))
                {
                    username = usernameBase + suffix++;
                }

                leadershipCredentials.Add(new Credential
                {
                    Username = username,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(DemoPassword),
                    Employee = e,
                    FailedLoginAttempts = 0,
                    IsLocked = false,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                });
            }

            ctx.Employees.AddRange(newManagersAndAdmins);
            ctx.Credentials.AddRange(leadershipCredentials);
            await ctx.SaveChangesAsync();

            // All baristas (existing + new) — Orders will rotate through these.
            var allBaristas = existingBaristas.Concat(newBaristas).ToList();
            var baristaIds = allBaristas.Select(b => b.EmployeeId).ToList();
            // allEmployees was captured before the new Manager/Admin were created,
            // so it doesn't include them. Re-query to get fresh IDs, then pick
            // the Manager and Admin we just made (or fall back to any existing one).
            var allEmployeesFresh = await ctx.Employees.AsNoTracking().ToListAsync();

            var managerId = allEmployeesFresh.FirstOrDefault(e =>
                e.Role.Equals("Manager", StringComparison.OrdinalIgnoreCase))?.EmployeeId
                ?? baristaIds[0];
            var adminId = allEmployeesFresh.FirstOrDefault(e =>
                e.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase))?.EmployeeId
                ?? managerId;

            // ---------- 3. Customers ----------

            var customerFaker = new Faker<Customer>()
                .RuleFor(c => c.FirstName, f => f.Name.FirstName())
                .RuleFor(c => c.LastName, f => f.Name.LastName())
                .RuleFor(c => c.ContactNumber, f => f.Phone.PhoneNumber("09#########").Replace("-", "").Replace(" ", ""))
                .RuleFor(c => c.Email, (f, c) => f.Internet.Email(c.FirstName, c.LastName).ToLower())
                .RuleFor(c => c.Birthday, f => f.Date.Between(
                    DateTime.Today.AddYears(-55),
                    DateTime.Today.AddYears(-16)))
                .RuleFor(c => c.LastVisitDate, f => f.Date.Recent(daysBack))
                .RuleFor(c => c.PointsBalance, f => f.Random.Number(0, 500))
                .RuleFor(c => c.IsActive, () => true)
                .RuleFor(c => c.CreatedAt, f => f.Date.Past(daysBack + 30))
                .RuleFor(c => c.UpdatedAt, () => DateTime.UtcNow);

            var newCustomers = customerFaker.Generate(customersToCreate);
            ctx.Customers.AddRange(newCustomers);
            await ctx.SaveChangesAsync();

            var customerIds = newCustomers.Select(c => c.CustomerId).ToList();

            // ---------- 4. Campaigns ----------

            var campaignFaker = new Faker<Campaign>()
                .RuleFor(c => c.PromoName, f => f.PickRandom(
                    "Weekend Matcha Blowout", "Cold Brew Days", "Loyalty Double Points",
                    "Birthday Treat", "We Miss You", "Grand Opening Special",
                    "Student Discount", "Flash Friday", "Iced Coffee Week",
                    "Matcha Madness", "Espresso Happy Hour", "Ube Season"))
                .RuleFor(c => c.PromoCategory, f => f.PickRandom("Seasonal", "Loyalty", "Birthday", "Inactive"))
                                .RuleFor(c => c.Reason, f => f.PickRandom(
                    "Weekend volume push", "Slow Tuesday traffic boost",
                    "Clear remaining matcha stock before new shipment",
                    "Reward loyal customers after positive feedback",
                    "Q3 revenue target shortfall", "Test new pricing strategy",
                    "Respond to competitor opening nearby",
                    "Holiday season customer appreciation",
                    "Push underperforming product line",
                    "Increase average order value through bundling"))
                .RuleFor(c => c.Description, f => f.PickRandom(
                    "Come by this weekend and enjoy a special treat on us — show this message at the counter.",
                    "We've got something for you. Drop in any day this week and mention this offer.",
                    "A little thank-you from us to you. Visit anytime before the offer ends.",
                    "Missing your usual? We saved a special deal just for you.",
                    "It's a good week to treat yourself. Come see what's brewing.",
                    "Something new is on the menu — come try it with a little extra off.",
                    "A warm cup is waiting. Drop by and let us know you got this message.",
                    "Thanks for being part of Morcaffein. Here's a small token of appreciation.",
                    "We've been thinking about you. Come in for a surprise deal.",
                    "Your next visit just got a little sweeter. Mention this offer at the counter."))
                .RuleFor(c => c.DiscountType, f => f.PickRandom("Percentage", "FixedAmount"))
                .RuleFor(c => c.DiscountValue, (f, c) =>
                    c.DiscountType == "Percentage" ? f.Random.Number(5, 25) : f.Random.Number(20, 100))
                .RuleFor(c => c.TargetTier, f => f.PickRandom("All", "Regular", "Silver", "Gold"))
                .RuleFor(c => c.MinPurchaseAmount, f => f.Random.Bool(0.4f) ? f.Random.Number(100, 300) : null)
                .RuleFor(c => c.MaxDiscountAmount, f => f.Random.Bool(0.4f) ? f.Random.Number(50, 200) : null)
                .RuleFor(c => c.MaxRedemptionsPerCustomer, f => f.Random.Number(1, 3))
                .RuleFor(c => c.MadeBy, () => managerId)
                .RuleFor(c => c.StartDate, f => f.Date.Recent(daysBack))
                .RuleFor(c => c.EndDate, (f, c) => c.StartDate.AddDays(f.Random.Number(7, 30)))
                .RuleFor(c => c.CreatedAt, f => f.Date.Recent(daysBack + 5))
                .RuleFor(c => c.UpdatedAt, () => DateTime.UtcNow);

            var campaigns = campaignFaker.Generate(15);

            // Assign statuses in a realistic mix.
            for (int i = 0; i < campaigns.Count; i++)
            {
                var c = campaigns[i];
                if (i < 8)
                {
                    c.Status = "Active";
                    c.IsActive = true;
                }
                else if (i < 11)
                {
                    c.Status = "Pending";
                    c.IsActive = false;
                }
                else if (i < 13)
                {
                    c.Status = "Rejected";
                    c.IsActive = false;
                    c.DecidedBy = adminId;
                    c.DecidedAt = c.CreatedAt.AddDays(1);
                    c.DecisionNotes = "Budget constraints this quarter.";
                }
                else
                {
                    c.Status = "Inactive";
                    c.IsActive = false;
                }
            }

            // Assign a target branch to each seeded campaign. Roughly 30% become
            // company-wide (BranchId = null) — redeemable at every branch — so the
            // demo shows a mix of scoped and global campaigns. The rest are
            // branch-scoped to a random branch.
            foreach (var c in campaigns)
            {
                var companyWide = rnd.Bool(0.30f);
                c.BranchId = companyWide
                    ? null
                    : rnd.ArrayElement(branchIds.ToArray());
            }

            ctx.Campaigns.AddRange(campaigns);
            await ctx.SaveChangesAsync();

            var activeCampaigns = campaigns.Where(c => c.Status == "Active").ToList();

            // ---------- 5. Orders + OrderItems + modifiers + redeems + ledger ----------

            var orders = new List<Order>();
            var orderItems = new List<OrderItem>();
            var redeems = new List<CampaignRedeem>();
            var redeemOwners = new List<Order>();   // parallel list — tracks which Order each redeem belongs to
            var ledgerEntries = new List<PointLedger>();

            // Reusable per-order faker.
            for (int i = 0; i < ordersToCreate; i++)
            {
                // Weight toward recent days so charts have shape.
                var daysAgo = GetWeightedDaysAgo(rnd, daysBack);
                // Build the date in shop-local time, then convert to UTC for storage.
                // The API treats Order.OrderDate as UTC and shifts +8 when displaying.
                var localHour = BogusHours(rnd);
                var phNow = DateTime.UtcNow.AddHours(8);   // "now" in shop-local time
                var localDate = phNow.Date.AddDays(-daysAgo)
                    .AddHours(localHour)
                    .AddMinutes(rnd.Number(0, 59));
                var orderDate = localDate.AddHours(-8);    // back to UTC for storage

                var customerId = rnd.ArrayElement(customerIds.ToArray());
                var baristaId = rnd.ArrayElement(baristaIds.ToArray());

                var order = new Order
                {
                    CustomerId = customerId,
                    CreatedBy = baristaId,
                    OrderDate = orderDate,
                    Status = "Completed",
                    IsActive = true,
                    CreatedAt = orderDate,
                    UpdatedAt = orderDate,
                    HasPwdSeniorDiscount = rnd.Bool(0.1f),
                    BranchId = rnd.ArrayElement(branchIds.ToArray()),

                };

                // 1-3 line items
                var itemCount = rnd.Number(1, 3);
                decimal subTotal = 0m;

                for (int j = 0; j < itemCount; j++)
                {
                    var size = rnd.ArrayElement(sizes.ToArray());
                    var quantity = rnd.Number(1, 2);

                    var item = new OrderItem
                    {
                        ProductId = size.ProductId,
                        ProductSizeId = size.ProductSizeId,
                        SizeNameSnapshot = size.SizeName,
                        BaseSizePrice = size.Price,
                        Quantity = quantity,
                    };

                    // 0-2 modifiers
                    var modCount = rnd.Number(0, 2);
                    var usedMods = new HashSet<int>();
                    decimal modifierTotal = 0m;

                    for (int k = 0; k < modCount; k++)
                    {
                        var mod = rnd.ArrayElement(modifiers.ToArray());
                        if (!usedMods.Add(mod.ProductModifierId)) continue;

                        item.OrderItemModifiers.Add(new OrderItemModifier
                        {
                            ProductModifierId = mod.ProductModifierId,
                            ProductModifierNameSnapshot = mod.Name,
                            UnitPrice = mod.Price,
                        });
                        modifierTotal += mod.Price;
                    }

                    item.UnitPrice = size.Price + modifierTotal;
                    item.SubTotal = item.UnitPrice * quantity;
                    subTotal += item.SubTotal;

                    order.OrderItems.Add(item);
                }

                order.SubTotal = subTotal;

                // Promo discount (optional)
                Campaign? appliedCampaign = null;
                decimal promoDiscount = 0m;
                if (activeCampaigns.Count > 0 && rnd.Bool(0.25f))
                {
                    appliedCampaign = rnd.ArrayElement(activeCampaigns.ToArray());
                    promoDiscount = CalculateDiscount(appliedCampaign, subTotal);

                    // Stage the redeem locally — we'll save it AFTER the Order has an
                    // identity, so OrderId can be set. Adding it to Order.CampaignRedeems
                    // here would cause EF to cascade-insert during the Orders save, which
                    // happens before OrderIds are assigned.
                    redeems.Add(new CampaignRedeem
                    {
                        CampaignId = appliedCampaign.CampaignId,
                        OrderId = 0,   // patched below, after Orders have IDs
                        DiscountUsed = promoDiscount,
                        RedeemDate = orderDate,
                        IsActive = true,
                        CreatedAt = orderDate,
                        UpdatedAt = orderDate,
                    });
                    redeemOwners.Add(order);
                }
                order.TotalDiscount = promoDiscount;

                // PWD/Senior stacking — same math as OrderController
                var afterPromo = Math.Max(0m, subTotal - promoDiscount);
                if (order.HasPwdSeniorDiscount)
                {
                    var netOfVat = afterPromo / 1.12m;
                    var pwdDiscount = decimal.Round(netOfVat * 0.20m, 2);
                    order.PwdSeniorDiscountAmount = pwdDiscount;
                    order.TotalAmount = decimal.Round(netOfVat - pwdDiscount, 2);
                }
                else
                {
                    order.PwdSeniorDiscountAmount = 0m;
                    order.TotalAmount = afterPromo;
                }

                order.PointsEarned = (int)(order.TotalAmount / 10m);
                orders.Add(order);
            }

            ctx.Orders.AddRange(orders);
            await ctx.SaveChangesAsync();

            // Now Orders have IDs — patch each redeem's OrderId from its owner, and
            // build ledger rows. The redeems were kept off the Orders' navigation
            // property on purpose, so we insert them here in a single clean pass.
            for (int i = 0; i < redeems.Count; i++)
            {
                redeems[i].OrderId = redeemOwners[i].OrderId;
            }

            foreach (var o in orders)
            {
                if (o.PointsEarned > 0)
                {
                    ledgerEntries.Add(new PointLedger
                    {
                        CustomerId = o.CustomerId,
                        PointsChange = o.PointsEarned,
                        Type = "Earned",
                        Reason = $"Order #{o.OrderId} Spend",
                        OrderId = o.OrderId,
                        Timestamp = o.OrderDate,
                        IsActive = true,
                        CreatedAt = o.OrderDate,
                        UpdatedAt = o.OrderDate,
                    });
                }
            }

            ctx.CampaignRedeems.AddRange(redeems);
            await ctx.SaveChangesAsync();

            // ---------- 6. Feedbacks ----------

            var feedbackFaker = new Faker<Feedback>()
                .RuleFor(f => f.CustomerId, f => f.PickRandom(customerIds))
                .RuleFor(f => f.LoggedBy, f => f.PickRandom(baristaIds))
                .RuleFor(f => f.BranchId, f => f.PickRandom(branchIds))
                .RuleFor(f => f.Rating, f => f.Random.WeightedRandom(new[] { 1, 2, 3, 4, 5 }, new[] { 0.03f, 0.05f, 0.12f, 0.40f, 0.40f }))
                .RuleFor(f => f.Category, f => f.PickRandom("Service", "Product Quality", "Cleanliness", "Wait Time", "Ambiance"))
                .RuleFor(f => f.StaffNotes, f => f.Lorem.Sentence(4))
                .RuleFor(f => f.Status, f => f.PickRandom("Pending", "Resolved", "Cancelled"))
                .RuleFor(f => f.DateLogged, f => f.Date.Recent(daysBack))
                .RuleFor(f => f.IsActive, () => true)
                .RuleFor(f => f.CreatedAt, (f, fb) => fb.DateLogged)
                .RuleFor(f => f.UpdatedAt, (f, fb) => fb.DateLogged);

            var feedbacks = feedbackFaker.Generate(30);

            // Resolved ones need DecidedBy / DecidedAt
            foreach (var fb in feedbacks.Where(f => f.Status == "Resolved"))
            {
                fb.DecidedBy = managerId;
                fb.DecidedAt = fb.DateLogged.AddDays(1);
                fb.DecisionNotes = rnd.Bool(0.5f) ? "Thanks for the feedback." : null;
            }

            ctx.Feedbacks.AddRange(feedbacks);
            await ctx.SaveChangesAsync();

            // Award bonus points for a subset of resolved feedbacks so the CSAT
            // Bonus Points metric on the Admin dashboard's Audit strip isn't
            // permanently zero. Skips roughly half of resolved tickets, which
            // mirrors the real behavior — the Manager resolves tickets but only
            // sometimes awards points as a goodwill gesture.
            foreach (var fb in feedbacks.Where(f => f.Status == "Resolved"))
            {
                if (!rnd.Bool(0.5f)) continue;

                var bonus = rnd.Number(5, 50);

                ledgerEntries.Add(new PointLedger
                {
                    CustomerId = fb.CustomerId,
                    PointsChange = bonus,
                    Type = "FeedbackBonus",
                    Reason = $"Feedback ticket #{fb.TicketId} — {fb.Category}",
                    PerformedBy = managerId,
                    Timestamp = fb.DecidedAt ?? fb.DateLogged,
                    IsActive = true,
                    CreatedAt = fb.DecidedAt ?? fb.DateLogged,
                    UpdatedAt = fb.DecidedAt ?? fb.DateLogged,
                });
            }

            // ---------- 7. Point adjustments ----------

            var adjustmentFaker = new Faker<PointAdjustmentRequest>()
                .RuleFor(a => a.CustomerId, f => f.PickRandom(customerIds))
                .RuleFor(a => a.PointsChange, f => f.Random.Bool(0.6f) ? f.Random.Number(5, 50) : -f.Random.Number(5, 30))
                .RuleFor(a => a.Reason, f => f.PickRandom(
                    "Goodwill for long wait", "Correction: missed points on prior order",
                    "Birthday bonus", "Compensation for wrong order", "Referral bonus"))
                .RuleFor(a => a.RequestedBy, f => f.PickRandom(baristaIds))
                .RuleFor(a => a.RequestedAt, f => f.Date.Recent(daysBack))
                .RuleFor(a => a.Status, f => f.PickRandom("Pending", "Approved", "Rejected"))
                .RuleFor(a => a.IsActive, () => true)
                .RuleFor(a => a.CreatedAt, (f, a) => a.RequestedAt)
                .RuleFor(a => a.UpdatedAt, (f, a) => a.RequestedAt);

            var adjustments = adjustmentFaker.Generate(12);

            foreach (var a in adjustments.Where(a => a.Status != "Pending"))
            {
                a.ApprovedBy = managerId;
                a.DecidedAt = a.RequestedAt.AddHours(rnd.Number(2, 48));
                if (a.Status == "Rejected")
                    a.DecisionNotes = "Not justified by policy.";
            }

            ctx.PointAdjustmentRequests.AddRange(adjustments);

            // ---------- 8. Add ledger rows for approved adjustments ----------

            foreach (var a in adjustments.Where(a => a.Status == "Approved"))
            {
                ledgerEntries.Add(new PointLedger
                {
                    CustomerId = a.CustomerId,
                    PointsChange = a.PointsChange,
                    Type = "Adjustment",
                    Reason = a.Reason,
                    PerformedBy = a.ApprovedBy,
                    Timestamp = a.DecidedAt ?? a.RequestedAt,
                    IsActive = true,
                    CreatedAt = a.DecidedAt ?? a.RequestedAt,
                    UpdatedAt = a.DecidedAt ?? a.RequestedAt,
                });
            }

            ctx.PointLedgers.AddRange(ledgerEntries);

            // ---------- 9. Update customer balances + last visit from ledger ----------

            await ctx.SaveChangesAsync();

            var balanceByCustomer = await ctx.PointLedgers
                .AsNoTracking()
                .GroupBy(pl => pl.CustomerId)
                .Select(g => new { CustomerId = g.Key, Total = g.Sum(x => x.PointsChange) })
                .ToDictionaryAsync(x => x.CustomerId, x => x.Total);

            var lastVisitByCustomer = await ctx.Orders
                .AsNoTracking()
                .GroupBy(o => o.CustomerId)
                .Select(g => new { CustomerId = g.Key, Latest = g.Max(x => x.OrderDate) })
                .ToDictionaryAsync(x => x.CustomerId, x => x.Latest);

            var seededCustomers = await ctx.Customers
                .Where(c => customerIds.Contains(c.CustomerId))
                .ToListAsync();

            foreach (var c in seededCustomers)
            {
                if (balanceByCustomer.TryGetValue(c.CustomerId, out var bal))
                    c.PointsBalance = bal;
                if (lastVisitByCustomer.TryGetValue(c.CustomerId, out var last))
                    c.LastVisitDate = last;
            }

            await ctx.SaveChangesAsync();
        }

        /// <summary>
        /// Populates the product catalog (products, sizes, modifiers) if the
        /// tenant's database doesn't have any yet. The catalog is identical
        /// across all tenants — a coffee shop's menu doesn't vary by location
        /// in this system — so seeding a fresh tenant just re-runs the same
        /// inserts. Safe to call repeatedly; short-circuits if products exist.
        /// </summary>
        private static async Task EnsureProductCatalogAsync(TenantCrmDbContext ctx)
        {
            if (await ctx.Products.AnyAsync())
                return;

            // ---- Product catalog ----
            // Fields: Name, Category, ImageUrl (Pexels reference), LocalImagePath
            // (file under Resources/Products/, relative to the app's base directory).
            // Hot/Cold variants share the same image file where only one exists —
            // visually imperfect but honest for a demo, and the ImageUrl column
            // already carries the correct asset for a future re-localization pass.
            var catalog = new (string Name, string Category, string ImageUrl, string? LocalImage)[]
            {
                // Matcha
                ("Matcha Latte (Hot)", "Matcha",
                    "https://images.pexels.com/photos/34334882/pexels-photo-34334882.jpeg",
                    "Resources/Products/matchahc.jpg"),
                ("Matcha Latte (Cold)", "Matcha",
                    "https://images.pexels.com/photos/34334882/pexels-photo-34334882.jpeg",
                    "Resources/Products/matchahc.jpg"),
                ("Pure Matcha (Hot)", "Matcha",
                    "https://images.pexels.com/photos/10846197/pexels-photo-10846197.jpeg",
                    "Resources/Products/purematchahc.jpg"),
                ("Pure Matcha (Cold)", "Matcha",
                    "https://images.pexels.com/photos/10846197/pexels-photo-10846197.jpeg",
                    "Resources/Products/purematchahc.jpg"),
                ("Vanilla Matcha (Hot)", "Matcha",
                    "https://images.pexels.com/photos/32300845/pexels-photo-32300845.jpeg",
                    "Resources/Products/vanillamatchahc.jpg"),
                ("Vanilla Matcha (Cold)", "Matcha",
                    "https://images.pexels.com/photos/32300845/pexels-photo-32300845.jpeg",
                    "Resources/Products/vanillamatchahc.jpg"),
                ("Honey Matcha (Hot)", "Matcha",
                    "https://images.pexels.com/photos/9309995/pexels-photo-9309995.jpeg",
                    "Resources/Products/honeymatchahc.jpg"),
                ("Honey Matcha (Cold)", "Matcha",
                    "https://images.pexels.com/photos/9309995/pexels-photo-9309995.jpeg",
                    "Resources/Products/honeymatchahc.jpg"),
                ("Strawberry Matcha (Hot)", "Matcha",
                    "https://images.pexels.com/photos/34491288/pexels-photo-34491288.jpeg",
                    "Resources/Products/strawberrymatchahc.jpg"),
                ("Strawberry Matcha (Cold)", "Matcha",
                    "https://images.pexels.com/photos/34491288/pexels-photo-34491288.jpeg",
                    "Resources/Products/strawberrymatchahc.jpg"),
                ("Sea Salt Matcha (Hot)", "Matcha",
                    "https://images.pexels.com/photos/33792899/pexels-photo-33792899.jpeg",
                    "Resources/Products/ssmatchahc.jpg"),
                ("Sea Salt Matcha (Cold)", "Matcha",
                    "https://images.pexels.com/photos/33792899/pexels-photo-33792899.jpeg",
                    "Resources/Products/ssmatchahc.jpg"),

                // Hot Coffee
                ("Espresso", "Hot Coffee",
                    "https://images.pexels.com/photos/2067432/pexels-photo-2067432.jpeg",
                    "Resources/Products/espresso.jpg"),
                ("Americano", "Hot Coffee",
                    "https://images.pexels.com/photos/25482738/pexels-photo-25482738.jpeg",
                    "Resources/Products/americano.jpg"),
                ("Cappuccino", "Hot Coffee",
                    "https://images.pexels.com/photos/7683595/pexels-photo-7683595.jpeg",
                    "Resources/Products/cappuccino.jpg"),
                ("Mocha", "Hot Coffee",
                    "https://images.pexels.com/photos/2226091/pexels-photo-2226091.jpeg",
                    "Resources/Products/mocha.jpg"),
                ("Caramel Macchiato", "Hot Coffee",
                    "https://images.pexels.com/photos/34969154/pexels-photo-34969154.jpeg",
                    "Resources/Products/caramelmac.jpg"),
                ("Spanish Latte", "Hot Coffee",
                    "https://images.pexels.com/photos/24294335/pexels-photo-24294335.jpeg",
                    "Resources/Products/espanyol.jpg"),
                ("Vietnamese Coffee", "Hot Coffee",
                    "https://images.pexels.com/photos/33684284/pexels-photo-33684284.jpeg",
                    "Resources/Products/vietnamese.jpg"),

                // Iced Coffee
                ("Iced Americano", "Iced Coffee",
                    "https://images.pexels.com/photos/25482738/pexels-photo-25482738.jpeg",
                    "Resources/Products/americano.jpg"),
                ("Iced Spanish Latte", "Iced Coffee",
                    "https://images.pexels.com/photos/24294335/pexels-photo-24294335.jpeg",
                    "Resources/Products/espanyol.jpg"),
                ("Iced Vietnamese Coffee", "Iced Coffee",
                    "https://images.pexels.com/photos/33684284/pexels-photo-33684284.jpeg",
                    "Resources/Products/vietnamese.jpg"),
                ("Iced Brown Sugar Oat Latte", "Iced Coffee",
                    "https://images.pexels.com/photos/36572437/pexels-photo-36572437.jpeg",
                    "Resources/Products/brownsugar.jpg"),
                ("Iced Caramel Macchiato", "Iced Coffee",
                    "https://images.pexels.com/photos/34969154/pexels-photo-34969154.jpeg",
                    "Resources/Products/caramelmac.jpg"),
                ("Iced Coconut Latte", "Iced Coffee",
                    "https://images.pexels.com/photos/6680480/pexels-photo-6680480.jpeg",
                    "Resources/Products/cocolatte.jpg"),
                ("Iced Hazelnut Latte", "Iced Coffee",
                    "https://images.pexels.com/photos/7463455/pexels-photo-7463455.jpeg",
                    "Resources/Products/hazelnut.jpg"),

                // Hot Non-Coffee
                ("Hot Chocolate (Classic)", "Hot Non-Coffee",
                    "https://images.pexels.com/photos/11043663/pexels-photo-11043663.jpeg",
                    "Resources/Products/hotchoco.jpg"),
                ("White Hot Chocolate", "Hot Non-Coffee",
                    "https://images.pexels.com/photos/6341422/pexels-photo-6341422.jpeg",
                    "Resources/Products/whitechoco.jpg"),
                ("Hot Ube Drink", "Hot Non-Coffee",
                    "https://images.pexels.com/photos/9754674/pexels-photo-9754674.jpeg",
                    "Resources/Products/ubeube.jpg"),
                ("Hot Salted Caramel Chocolate", "Hot Non-Coffee",
                    "https://images.pexels.com/photos/12065521/pexels-photo-12065521.jpeg",
                    "Resources/Products/scc.jpg"),

                // Cold Non-Coffee
                ("Iced Chocolate (Classic)", "Cold Non-Coffee",
                    "https://images.pexels.com/photos/11043663/pexels-photo-11043663.jpeg",
                    "Resources/Products/hotchoco.jpg"),
                ("Iced White Chocolate", "Cold Non-Coffee",
                    "https://images.pexels.com/photos/6341422/pexels-photo-6341422.jpeg",
                    "Resources/Products/whitechoco.jpg"),
                ("Iced Ube Drink", "Cold Non-Coffee",
                    "https://images.pexels.com/photos/9754674/pexels-photo-9754674.jpeg",
                    "Resources/Products/ubeube.jpg"),
                ("Iced Salted Caramel Chocolate", "Cold Non-Coffee",
                    "https://images.pexels.com/photos/12065521/pexels-photo-12065521.jpeg",
                    "Resources/Products/scc.jpg"),
            };

            int codeCounter = 1;
            foreach (var (name, category, imageUrl, localImage) in catalog)
            {
                var product = new Product
                {
                    ProductCode = $"P{codeCounter:D3}",
                    ProductName = name,
                    ProductCategory = category,
                    BasePrice = 39m,
                    ImageUrl = imageUrl,
                    LocalImagePath = localImage,
                    IsAvailable = true,
                };

                // Uniform pricing across the menu: Regular ₱39, Large ₱49, XL ₱59.
                product.ProductSizes.Add(new ProductSize { SizeName = "Regular", Price = 39m });
                product.ProductSizes.Add(new ProductSize { SizeName = "Large", Price = 49m });
                product.ProductSizes.Add(new ProductSize { SizeName = "XL", Price = 59m });

                ctx.Products.Add(product);
                codeCounter++;
            }

            // ---- Modifier catalog ----
            var modifiers = new (string Name, string Category, decimal Price)[]
            {
                ("Whole Milk", "Milk Swap", 0m),
                ("Oat Milk", "Milk Swap", 20m),
                ("Almond Milk", "Milk Swap", 20m),
                ("Soy Milk", "Milk Swap", 20m),

                ("Extra Espresso Shot", "Extra Shot", 40m),
                ("Extra Matcha Scoop", "Extra Shot", 50m),

                ("Vanilla Syrup", "Syrup", 10m),
                ("Caramel Syrup", "Syrup", 10m),
                ("Hazelnut Syrup", "Syrup", 10m),
                ("Brown Sugar Syrup", "Syrup", 10m),
                ("Sea Salt Syrup", "Syrup", 10m),
                ("Ube Syrup", "Syrup", 10m),

                ("Whipped Cream", "Topping", 30m),
                ("Cold Foam", "Topping", 30m),
                ("Marshmallows", "Topping", 20m),
                ("Cocoa Dust", "Topping", 5m),
                ("Cinnamon Dust", "Topping", 5m),

                ("Less Ice", "Prep", 0m),
                ("Extra Ice", "Prep", 0m),
                ("No Sugar", "Prep", 0m),
                ("Half Sugar", "Prep", 0m),
            };

            foreach (var (name, category, price) in modifiers)
            {
                ctx.ProductModifiers.Add(new ProductModifier
                {
                    Name = name,
                    Category = category,
                    Price = price,
                });
            }

            await ctx.SaveChangesAsync();
        }

        // ============================================================
        // Helpers
        // ============================================================

        /// <summary>
        /// Weighted day distribution — more orders on recent days so charts have a
        /// visible trend. Simple linear bias: recent days get more weight.
        /// </summary>
        private static int GetWeightedDaysAgo(Bogus.Randomizer rnd, int daysBack)
        {
            var r = rnd.Double();
            // Bias toward 0 (today) with exponential-ish decay.
            var daysAgo = (int)(daysBack * Math.Pow(1 - r, 1.6));
            return Math.Clamp(daysAgo, 0, daysBack - 1);
        }

        /// <summary>Shop-hours-weighted hour of day (peak at 8-10am and 2-4pm).</summary>
        private static int BogusHours(Bogus.Randomizer rnd)
        {
            // Simple two-peak distribution: 7am-9pm open hours, peak mid-morning/afternoon.
            var r = rnd.Number(0, 99);
            if (r < 20) return rnd.Number(7, 10);    // morning peak
            if (r < 40) return rnd.Number(11, 13);   // lunch
            if (r < 70) return rnd.Number(14, 17);   // afternoon peak
            return rnd.Number(18, 21);               // evening
        }

        /// <summary>Mirrors OrderController's promo discount math.</summary>
        private static decimal CalculateDiscount(Campaign campaign, decimal subTotal)
        {
            if (campaign.MinPurchaseAmount.HasValue && subTotal < campaign.MinPurchaseAmount.Value)
                return 0m;

            decimal discount;
            if (campaign.DiscountType.Equals("Percentage", StringComparison.OrdinalIgnoreCase))
            {
                discount = subTotal * (campaign.DiscountValue / 100m);
                if (campaign.MaxDiscountAmount.HasValue && discount > campaign.MaxDiscountAmount.Value)
                    discount = campaign.MaxDiscountAmount.Value;
            }
            else
            {
                discount = campaign.DiscountValue;
            }

            return discount > subTotal ? subTotal : discount;
        }
    }
}
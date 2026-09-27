using CRM_Morcaffein.domain.entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CRM_Morcaffein.infrastructure.data;

public class TenantCrmDbContext : IdentityDbContext
{
    // ===== TENANT CRM DOMAIN TABLES =====
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductSize> ProductSizes => Set<ProductSize>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Credential> Credentials => Set<Credential>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<CampaignRedeem> CampaignRedeems => Set<CampaignRedeem>();
    public DbSet<Feedback> Feedbacks => Set<Feedback>();
    public DbSet<PointLedger> PointLedgers => Set<PointLedger>();
    public DbSet<ProductModifier> ProductModifiers => Set<ProductModifier>();
    public DbSet<OrderItemModifier> OrderItemModifiers => Set<OrderItemModifier>();
    public DbSet<PointAdjustmentRequest> PointAdjustmentRequests => Set<PointAdjustmentRequest>();
    public DbSet<Branch> Branches => Set<Branch>();


    public TenantCrmDbContext(DbContextOptions<TenantCrmDbContext> options) : base(options)
    { }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.AddInterceptors(new AuditInterceptor());
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // 0. BRANCH
        builder.Entity<Branch>(entity =>
        {
            entity.HasKey(b => b.BranchId);
            entity.Property(b => b.BranchName).HasMaxLength(100).IsRequired();
            entity.Property(b => b.Address).HasMaxLength(250);
        });

        // 1. PRODUCT
        builder.Entity<Product>(entity =>
        {
            entity.HasKey(p => p.ProductId);

            entity.Property(p => p.ProductCode)
                  .HasMaxLength(50)
                  .IsRequired();

            entity.Property(p => p.ProductName)
                  .HasMaxLength(200)
                  .IsRequired();

            entity.Property(p => p.ImageUrl).HasMaxLength(500);
            entity.Property(p => p.LocalImagePath).HasMaxLength(500);

            entity.Property(p => p.BasePrice)
                  .HasPrecision(18, 2);

            entity.HasIndex(p => p.ProductCode)
                  .IsUnique();
        });

        builder.Entity<ProductModifier>(entity =>
        {
            entity.HasKey(pm => pm.ProductModifierId);
            entity.Property(pm => pm.Name).HasMaxLength(100).IsRequired();
            entity.Property(pm => pm.Category).HasMaxLength(50);
            entity.Property(pm => pm.Price).HasPrecision(18, 2);
        });

        builder.Entity<OrderItemModifier>(entity =>
        {
            entity.HasKey(oim => oim.OrderItemModifierId);
            entity.Property(oim => oim.ProductModifierNameSnapshot).HasMaxLength(100);
            entity.Property(oim => oim.UnitPrice).HasPrecision(18, 2);

            entity.HasOne(oim => oim.OrderItem)
                  .WithMany(oi => oi.OrderItemModifiers)
                  .HasForeignKey(oim => oim.OrderItemId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(oim => oim.ProductModifier)
                  .WithMany()
                  .HasForeignKey(oim => oim.ProductModifierId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // 2. PRODUCT SIZE
        builder.Entity<ProductSize>(entity =>
        {
            entity.HasKey(ps => ps.ProductSizeId);

            entity.Property(ps => ps.SizeName)
                  .HasMaxLength(50)
                  .IsRequired();

            entity.Property(ps => ps.Price)
                  .HasPrecision(18, 2);

            entity.HasOne(ps => ps.Product)
                  .WithMany(p => p.ProductSizes)
                  .HasForeignKey(ps => ps.ProductId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 3. ORDER
        builder.Entity<Order>(entity =>
        {
            entity.HasKey(o => o.OrderId);
            entity.Property(o => o.SubTotal).HasPrecision(18, 2);
            entity.Property(o => o.TotalDiscount).HasPrecision(18, 2);
            entity.Property(o => o.TotalAmount).HasPrecision(18, 2);
            entity.Property(o => o.PointsEarned).IsRequired();
            entity.Property(o => o.BranchId).IsRequired();

            entity.HasOne(o => o.Customer)
                  .WithMany(c => c.Orders)
                  .HasForeignKey(o => o.CustomerId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(o => o.Employee)
                  .WithMany()
                  .HasForeignKey(o => o.CreatedBy)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(o => o.Branch)
                  .WithMany()
                  .HasForeignKey(o => o.BranchId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(o => o.BranchId);

        });

        // 4. ORDER ITEM
        builder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(oi => oi.OrderItemId);

            // Stored Properties
            entity.Property(oi => oi.BaseSizePrice).HasPrecision(18, 2);
            entity.Property(oi => oi.SizeNameSnapshot).HasMaxLength(50);

            // Dynamic Computed Properties (Not stored directly in DB table)
            entity.Ignore(oi => oi.UnitPrice);
            entity.Ignore(oi => oi.SubTotal);

            // Relationships
            entity.HasOne(oi => oi.Order)
                  .WithMany(o => o.OrderItems)
                  .HasForeignKey(oi => oi.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(oi => oi.Product)
                  .WithMany(p => p.OrderItems)
                  .HasForeignKey(oi => oi.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(oi => oi.ProductSize)
                  .WithMany()
                  .HasForeignKey(oi => oi.ProductSizeId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // 5. CAMPAIGN

        builder.Entity<Campaign>(entity =>
        {
            entity.HasKey(c => c.CampaignId);
            entity.Property(c => c.PromoName).HasMaxLength(200).IsRequired();
            entity.Property(c => c.PromoCategory).HasMaxLength(100).IsRequired();
            entity.Property(c => c.Status).HasMaxLength(50).IsRequired();
            entity.Property(c => c.Reason).HasMaxLength(250).IsRequired().HasDefaultValue("");
            entity.Property(c => c.Description).HasMaxLength(500);
            entity.Property(c => c.DecisionNotes).HasMaxLength(500);
            entity.Property(c => c.DiscountValue).HasPrecision(18, 2);

            entity.HasOne(c => c.Creator)
                  .WithMany()
                  .HasForeignKey(c => c.MadeBy)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.Decider)
                  .WithMany()
                  .HasForeignKey(c => c.DecidedBy)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.Branch)
                  .WithMany()
                  .HasForeignKey(c => c.BranchId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(c => c.BranchId);
        });

        // 6. CAMPAIGN REDEEM
        builder.Entity<CampaignRedeem>(entity =>
        {
            entity.HasKey(cr => cr.RedeemId);
            entity.Property(cr => cr.DiscountUsed).HasPrecision(18, 2);

            entity.HasOne(cr => cr.Campaign)
                  .WithMany(c => c.CampaignRedeems)
                  .HasForeignKey(cr => cr.CampaignId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(cr => cr.Order)
                  .WithMany(o => o.CampaignRedeems)
                  .HasForeignKey(cr => cr.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 7. FEEDBACK
        builder.Entity<Feedback>(entity =>
        {
            entity.HasKey(f => f.TicketId);
            entity.Property(f => f.Category).HasMaxLength(100);
            entity.Property(f => f.Status).HasMaxLength(50);
            entity.Property(f => f.DecisionNotes).HasMaxLength(500);
            entity.Property(f => f.BranchId).IsRequired();

            entity.HasOne(f => f.Customer)
                  .WithMany(c => c.Feedbacks)
                  .HasForeignKey(f => f.CustomerId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(f => f.LoggedByEmployee)
                  .WithMany()
                  .HasForeignKey(f => f.LoggedBy)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(f => f.DecidedByEmployee)
                  .WithMany()
                  .HasForeignKey(f => f.DecidedBy)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(f => f.Branch)
                  .WithMany()
                  .HasForeignKey(f => f.BranchId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(f => f.BranchId);
        });

        // 8. POINT LEDGER
        builder.Entity<PointLedger>(entity =>
        {
            entity.HasKey(pl => pl.PointLedgerId);
            entity.Property(pl => pl.Type).HasMaxLength(50).IsRequired();
            entity.Property(pl => pl.Reason).HasMaxLength(250);

            entity.HasOne(pl => pl.Customer)
                  .WithMany(c => c.PointLedgers)
                  .HasForeignKey(pl => pl.CustomerId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(pl => pl.Order)
                  .WithMany()
                  .HasForeignKey(pl => pl.OrderId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(pl => pl.PerformedByEmployee)
                  .WithMany()
                  .HasForeignKey(pl => pl.PerformedBy)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // 9. POINT ADJUSTMENT REQUEST
        builder.Entity<PointAdjustmentRequest>(entity =>
        {
            entity.HasKey(r => r.PointAdjustmentRequestId);
            entity.Property(r => r.Reason).HasMaxLength(250).IsRequired();
            entity.Property(r => r.Status).HasMaxLength(50).IsRequired();
            entity.Property(r => r.DecisionNotes).HasMaxLength(250);

            entity.HasOne(r => r.Customer)
                  .WithMany(c => c.PointAdjustmentRequests)
                  .HasForeignKey(r => r.CustomerId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(r => r.Requester)
                  .WithMany()
                  .HasForeignKey(r => r.RequestedBy)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.Approver)
                  .WithMany()
                  .HasForeignKey(r => r.ApprovedBy)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.EmployeeId);
            entity.HasOne(e => e.Branch)
                  .WithMany()
                  .HasForeignKey(e => e.BranchId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
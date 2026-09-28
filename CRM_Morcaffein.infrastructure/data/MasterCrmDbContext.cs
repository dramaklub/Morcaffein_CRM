using CRM_Morcaffein.domain.entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;



namespace CRM_Morcaffein.infrastructure.data
{
    public class MasterCrmDbContext : IdentityDbContext
    {
        public DbSet<Company> Companies => Set<Company>();
        public DbSet<CompanyDatabase> CompanyDatabases => Set<CompanyDatabase>();
        public DbSet<Device> Devices => Set<Device>();
        public DbSet<Plan> Plans => Set<Plan>();
        public DbSet<Subscription> Subscriptions => Set<Subscription>();
        public DbSet<CompanyUser> CompanyUsers => Set<CompanyUser>();
        public DbSet<SuperAdmin> SuperAdmins => Set<SuperAdmin>();
        public DbSet<TermsAndConditions> TermsAndConditions => Set<TermsAndConditions>();


        public MasterCrmDbContext(DbContextOptions<MasterCrmDbContext> options) : base(options)
        { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);

            optionsBuilder.AddInterceptors(new AuditInterceptor());
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Company>(entity =>

            {

                entity.HasKey(x => x.CompanyId);



                entity.Property(x => x.CompanyCode)

                  .HasMaxLength(50)

                  .IsRequired();



                entity.Property(x => x.CompanyName)

                  .HasMaxLength(200)

                  .IsRequired();



                entity.HasIndex(x => x.CompanyCode)

                  .IsUnique();

                entity.HasOne(c => c.CurrentPlan)
                  .WithMany()
                  .HasForeignKey(c => c.CurrentPlanId)
                  .OnDelete(DeleteBehavior.Restrict);

            });



            builder.Entity<CompanyDatabase>(entity =>

            {

                entity.HasKey(x => x.CompanyDatabaseId);



                entity.Property(x => x.ServerName)

                  .HasMaxLength(200)

                  .IsRequired();



                entity.Property(x => x.DatabaseName)

                  .HasMaxLength(200)

                  .IsRequired();



                entity.HasOne(x => x.Company)

                  .WithMany()

                  .HasForeignKey(x => x.CompanyId)

                  .OnDelete(DeleteBehavior.Restrict);

                entity.Property(cd => cd.CloudServerName).HasMaxLength(200);
                entity.Property(cd => cd.CloudDatabaseName).HasMaxLength(200);

            });

            builder.Entity<Device>(entity =>
            {
                entity.HasKey(x => x.DeviceId);

                entity.Property(x => x.DeviceCode)
                .HasMaxLength(50).IsRequired();

                entity.Property(x => x.DeviceName)
                .HasMaxLength(200).IsRequired();

                entity.HasOne(x => x.Company)
                .WithMany(x => x.Devices)
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(x => new { x.CompanyId, x.DeviceCode })
                .IsUnique();

            });

            builder.Entity<Plan>(entity =>
            {
                entity.HasKey(p => p.PlanId);

                entity.Property(p => p.Name)
                      .HasMaxLength(50)
                      .IsRequired();

                entity.Property(p => p.Description)
                      .HasMaxLength(250);

                entity.Property(p => p.ModulesCsv)
                      .HasMaxLength(500)
                      .IsRequired();

                entity.Property(p => p.MonthlyPrice)
                      .HasPrecision(18, 2);

                entity.HasIndex(p => p.Name)
                        .IsUnique();
            });

            builder.Entity<Subscription>(entity =>
            {
                entity.HasKey(s => s.SubscriptionId);

                entity.Property(s => s.TermsAcceptedBy)
                      .HasMaxLength(200);

                entity.HasOne(s => s.Company)
                      .WithMany()
                      .HasForeignKey(s => s.CompanyId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(s => s.Plan)
                      .WithMany()
                      .HasForeignKey(s => s.PlanId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Index on CompanyId because the "get current subscription for
                // company N" query is the most common one.
                entity.HasIndex(s => s.CompanyId);
            });

            builder.Entity<TermsAndConditions>(entity =>
            {
                entity.HasKey(t => t.TermsId);

                entity.Property(t => t.Title)
                      .HasMaxLength(200)
                      .IsRequired();

                entity.Property(t => t.Body)
                      .IsRequired();     // nvarchar(max) — no length cap

                entity.Property(t => t.Version)
                      .HasMaxLength(20)
                      .IsRequired();

                entity.HasOne(t => t.Plan)
                      .WithMany()
                      .HasForeignKey(t => t.PlanId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(t => t.PlanId);
            });

            builder.Entity<SuperAdmin>(entity =>
            {
                entity.HasKey(s => s.SuperAdminId);

                entity.Property(s => s.Username)
                      .HasMaxLength(50)
                      .IsRequired();

                entity.Property(s => s.PasswordHash)
                      .HasMaxLength(200)
                      .IsRequired();

                entity.Property(s => s.FullName)
                      .HasMaxLength(200);

                entity.Property(s => s.Email)
                      .HasMaxLength(200);

                entity.HasIndex(s => s.Username)
                      .IsUnique();
            });

            builder.Entity<CompanyUser>(entity =>
            {
                entity.HasKey(cu => cu.CompanyUserId);

                entity.Property(cu => cu.Username)
                      .HasMaxLength(50)
                      .IsRequired();

                entity.HasIndex(cu => cu.Username)
                      .IsUnique();

                entity.HasOne(cu => cu.Company)
                      .WithMany()
                      .HasForeignKey(cu => cu.CompanyId)
                      .OnDelete(DeleteBehavior.Restrict);
            });


        }

    }

}


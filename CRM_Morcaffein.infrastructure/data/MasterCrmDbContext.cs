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
        }

    }

}


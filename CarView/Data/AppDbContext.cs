using CarView.Models;
using CarView.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CarView.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        private readonly ITenantService _tenantService;
        private int CurrentTenantId => _tenantService.GetTenantId();
        public AppDbContext(DbContextOptions<AppDbContext> options, ITenantService tenantService) : base(options)
        {
            _tenantService = tenantService;
        }
        public DbSet<Tenant> Tenants { get; set; }

        public DbSet<Brand> Brands { get; set; }
        public DbSet<Car> Cars { get; set; }
        public DbSet<CarMedia> CarMedias { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Car>().HasQueryFilter(c => CurrentTenantId == 0 || c.TenantId == CurrentTenantId);

            modelBuilder.Entity<CarMedia>().HasQueryFilter(m => CurrentTenantId == 0 || m.Car.TenantId == CurrentTenantId);

            modelBuilder.Entity<Brand>()
                .HasMany(b => b.Cars)
                .WithOne(c => c.Brand)
                .HasForeignKey(c => c.BrandId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            foreach (var entry in ChangeTracker.Entries<ITenantEntity>().Where(e => e.State == EntityState.Added))
            {
                if (CurrentTenantId != 0)
                {
                    entry.Entity.TenantId = CurrentTenantId;
                }
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }
}

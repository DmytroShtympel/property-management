using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Domain.Entities;

namespace PropertyManagement.Infrastructure.Persistence;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Property> Properties => Set<Property>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<UnitType> UnitTypes => Set<UnitType>();
    public DbSet<RentalApplication> RentalApplications => Set<RentalApplication>();
    public DbSet<ApplicationApplicant> ApplicationApplicants => Set<ApplicationApplicant>();
    public DbSet<ApplicantInformation> ApplicantInformations => Set<ApplicantInformation>();
    public DbSet<ResidenceHistoryEntry> ResidenceHistoryEntries => Set<ResidenceHistoryEntry>();
    public DbSet<ApplicationStatusHistoryEntry> ApplicationStatusHistoryEntries => Set<ApplicationStatusHistoryEntry>();
    public DbSet<ApplicationNote> ApplicationNotes => Set<ApplicationNote>();
    public DbSet<Lease> Leases => Set<Lease>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        BumpConcurrencyTokens();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        BumpConcurrencyTokens();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>App-managed optimistic-concurrency tokens are incremented here, once, for
    /// every entity that carries one, rather than scattering `Version++` calls through the
    /// application/controller layer. EF Core's InMemory provider can exercise this (unlike a
    /// SQL Server-native rowversion column), which is what makes the concurrency-conflict
    /// scenarios in the spec unit-testable.</summary>
    private void BumpConcurrencyTokens()
    {
        foreach (var entry in ChangeTracker.Entries<RentalApplication>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.ClaimVersion++;
            }
        }

        foreach (var entry in ChangeTracker.Entries<ApplicantInformation>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.Version++;
            }
        }

        foreach (var entry in ChangeTracker.Entries<ResidenceHistoryEntry>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.Version++;
            }
        }
    }
}

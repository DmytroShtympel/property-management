using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyManagement.Domain.Entities;

namespace PropertyManagement.Infrastructure.Persistence.Configurations;

public class LeaseConfiguration : IEntityTypeConfiguration<Lease>
{
    public void Configure(EntityTypeBuilder<Lease> builder)
    {
        builder.Property(l => l.MonthlyRent).HasColumnType("decimal(10,2)");
        builder.HasIndex(l => l.RentalApplicationId).IsUnique();

        builder.HasOne(l => l.Unit)
            .WithMany(u => u.Leases)
            .HasForeignKey(l => l.UnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyManagement.Domain.Entities;

namespace PropertyManagement.Infrastructure.Persistence.Configurations;

public class UnitConfiguration : IEntityTypeConfiguration<Unit>
{
    public void Configure(EntityTypeBuilder<Unit> builder)
    {
        builder.Property(u => u.UnitNumber).IsRequired().HasMaxLength(50);
        builder.Property(u => u.RentAmount).HasColumnType("decimal(10,2)");

        builder.HasOne(u => u.Property)
            .WithMany(p => p.Units)
            .HasForeignKey(u => u.PropertyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(u => u.UnitType)
            .WithMany(t => t.Units)
            .HasForeignKey(u => u.UnitTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Hidden once the unit itself OR its parent property is soft-removed.
        builder.HasQueryFilter(u => !u.IsRemoved && !u.Property!.IsRemoved);
    }
}

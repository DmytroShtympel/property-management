using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyManagement.Domain.Entities;

namespace PropertyManagement.Infrastructure.Persistence.Configurations;

public class RentalApplicationConfiguration : IEntityTypeConfiguration<RentalApplication>
{
    public void Configure(EntityTypeBuilder<RentalApplication> builder)
    {
        builder.Property(a => a.ClaimVersion).IsConcurrencyToken();

        builder.HasOne(a => a.Unit)
            .WithMany(u => u.Applications)
            .HasForeignKey(a => a.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.ClaimedByUser)
            .WithMany()
            .HasForeignKey(a => a.ClaimedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.ApplicantInformation)
            .WithOne(i => i.RentalApplication!)
            .HasForeignKey<ApplicantInformation>(i => i.RentalApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(a => a.Applicants)
            .WithOne(x => x.RentalApplication)
            .HasForeignKey(x => x.RentalApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(a => a.ResidenceHistoryEntries)
            .WithOne(x => x.RentalApplication)
            .HasForeignKey(x => x.RentalApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(a => a.StatusHistory)
            .WithOne(x => x.RentalApplication)
            .HasForeignKey(x => x.RentalApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(a => a.Notes)
            .WithOne(x => x.RentalApplication)
            .HasForeignKey(x => x.RentalApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Lease)
            .WithOne(l => l.RentalApplication!)
            .HasForeignKey<Lease>(l => l.RentalApplicationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

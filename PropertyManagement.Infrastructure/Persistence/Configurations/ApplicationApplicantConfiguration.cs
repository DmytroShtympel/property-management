using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyManagement.Domain.Entities;

namespace PropertyManagement.Infrastructure.Persistence.Configurations;

public class ApplicationApplicantConfiguration : IEntityTypeConfiguration<ApplicationApplicant>
{
    public void Configure(EntityTypeBuilder<ApplicationApplicant> builder)
    {
        builder.HasIndex(x => new { x.RentalApplicationId, x.UserId }).IsUnique();

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

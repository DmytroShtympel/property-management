using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyManagement.Domain.Entities;

namespace PropertyManagement.Infrastructure.Persistence.Configurations;

public class ApplicantInformationConfiguration : IEntityTypeConfiguration<ApplicantInformation>
{
    public void Configure(EntityTypeBuilder<ApplicantInformation> builder)
    {
        builder.HasIndex(i => i.RentalApplicationId).IsUnique();
        builder.Property(i => i.Version).IsConcurrencyToken();
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyManagement.Domain.Entities;

namespace PropertyManagement.Infrastructure.Persistence.Configurations;

public class ResidenceHistoryEntryConfiguration : IEntityTypeConfiguration<ResidenceHistoryEntry>
{
    public void Configure(EntityTypeBuilder<ResidenceHistoryEntry> builder)
    {
        builder.Property(e => e.Version).IsConcurrencyToken();
    }
}

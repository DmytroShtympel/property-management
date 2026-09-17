using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyManagement.Domain.Entities;

namespace PropertyManagement.Infrastructure.Persistence.Configurations;

public class ApplicationStatusHistoryEntryConfiguration : IEntityTypeConfiguration<ApplicationStatusHistoryEntry>
{
    public void Configure(EntityTypeBuilder<ApplicationStatusHistoryEntry> builder)
    {
        builder.HasOne(e => e.ChangedByUser)
            .WithMany()
            .HasForeignKey(e => e.ChangedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

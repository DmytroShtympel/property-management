using Microsoft.EntityFrameworkCore;
using PropertyManagement.Infrastructure.Persistence;

namespace PropertyManagement.Tests.Infrastructure;

internal static class InMemoryDbContextFactory
{
    public static ApplicationDbContext Create() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}

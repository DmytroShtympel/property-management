using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Infrastructure.Persistence;

namespace PropertyManagement.Infrastructure.Seed;

/// <summary>Orchestrates "on start: create the database, apply migrations, and seed
/// idempotently" per the spec. Each phase is independently guarded so a partially-seeded
/// database (e.g. roles exist but no properties yet) still fills gaps on the next run instead
/// of either duplicating data or silently doing nothing.</summary>
public static class DbInitializer
{
    public const string DemoPassword = "Passw0rd1!";

    public static async Task SeedAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        await SeedRolesAsync(roleManager);
        var manager1 = await SeedFixedUserAsync(userManager, "manager1@demo.local", "Morgan", "Reyes", Roles.PropertyManager);
        var manager2 = await SeedFixedUserAsync(userManager, "manager2@demo.local", "Priya", "Anand", Roles.PropertyManager);
        var applicant1 = await SeedFixedUserAsync(userManager, "applicant1@demo.local", "Jordan", "Lee", Roles.Applicant);
        var applicant2 = await SeedFixedUserAsync(userManager, "applicant2@demo.local", "Casey", "Kim", Roles.Applicant);

        var unitTypes = await SeedUnitTypesAsync(db);

        if (!await db.Properties.IgnoreQueryFilters().AnyAsync())
        {
            await BogusDataSeeder.SeedAsync(db, userManager, manager1, manager2, applicant1, applicant2, unitTypes);
        }
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    private static async Task<ApplicationUser> SeedFixedUserAsync(UserManager<ApplicationUser> userManager, string email, string firstName, string lastName, string role)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            return existing;
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = firstName,
            LastName = lastName
        };

        var result = await userManager.CreateAsync(user, DemoPassword);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Failed to seed user '{email}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        await userManager.AddToRoleAsync(user, role);
        return user;
    }

    private static async Task<List<UnitType>> SeedUnitTypesAsync(ApplicationDbContext db)
    {
        if (await db.UnitTypes.AnyAsync())
        {
            return await db.UnitTypes.ToListAsync();
        }

        var types = new List<UnitType>
        {
            new() { Name = "Studio", IsActive = true },
            new() { Name = "1 Bedroom", IsActive = true },
            new() { Name = "2 Bedroom", IsActive = true },
            new() { Name = "3 Bedroom", IsActive = true },
            new() { Name = "Townhouse", IsActive = true },
            // Seeded inactive on purpose so the "inactive lookup" behavior is visible without
            // the reviewer needing to toggle it themselves.
            new() { Name = "Loft", IsActive = false }
        };

        db.UnitTypes.AddRange(types);
        await db.SaveChangesAsync();
        return types;
    }
}

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Domain.Services;
using PropertyManagement.Infrastructure.Services;

namespace PropertyManagement.Infrastructure.Persistence.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<ApplicationWorkflowService>();
        services.AddScoped<ApplicationClaimService>();

        services.AddScoped<IPropertyService, PropertyService>();
        services.AddScoped<IUnitService, UnitService>();
        services.AddScoped<IUnitTypeService, UnitTypeService>();
        services.AddScoped<IListingService, ListingService>();
        services.AddScoped<IApplicationService, ApplicationService>();
        services.AddScoped<IApplicationReviewService, ApplicationReviewService>();

        return services;
    }
}

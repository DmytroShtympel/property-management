using Microsoft.AspNetCore.Authorization;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Infrastructure.Persistence.DependencyInjection;
using PropertyManagement.Infrastructure.Seed;
using PropertyManagement.Web.Authorization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllersWithViews();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ManagerOnly", p => p.RequireRole(Roles.PropertyManager));
    options.AddPolicy("ApplicantOnly", p => p.RequireRole(Roles.Applicant));
});
builder.Services.AddSingleton<IAuthorizationHandler, PropertyOwnerHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, ApplicationAccessHandler>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";

    // AD-15: browser MVC actions redirect to a friendly page on denial, but the bonus FR-19 JSON
    // API has no page to redirect a caller to — it gets a raw status code instead, per REST
    // convention. Without this, an unauthorized /api/* request gets a 302 to an HTML login page.
    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        }
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "PropertyManagement API",
        Version = "v1",
        Description = "Paged/sorted access to rental applications."
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseSwagger();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "PropertyManagement API v1"));
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    await DbInitializer.SeedAsync(scope.ServiceProvider);
}

app.Run();

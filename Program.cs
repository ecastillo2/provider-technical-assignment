using Microsoft.EntityFrameworkCore;
using ProviderAssignmentStarter.Infrastructure.Data;
using ProviderAssignmentStarter.Infrastructure.Interceptors;
using ProviderAssignmentStarter.Infrastructure.Repositories;
using ProviderAssignmentStarter.Services;

// =====================================================================
// Application bootstrap.
//
// The composition root for the application. Every service registration,
// every interceptor wiring, and every middleware in the HTTP pipeline
// happens here. The order of operations matters and is documented inline.
//
// Sections:
//   1. Cross-cutting interceptors (singletons, stateless)
//   2. EF Core / SQLite registration
//   3. Application services (repositories, services, dashboard)
//   4. MVC
//   5. DB initialisation (creates schema + seeds on first run)
//   6. HTTP request pipeline
// =====================================================================

var builder = WebApplication.CreateBuilder(args);

// ---------- 1. Cross-cutting interceptors ----------
//
// Both interceptors are stateless and safe to share across DbContext
// instances, so we register them as singletons. AppDbContext picks them
// up via opts.AddInterceptors(...) below.
//
// Order of registration matters at SaveChanges time:
//   - SoftDeleteInterceptor runs FIRST and rewrites Deleted -> Modified.
//   - AuditableInterceptor runs SECOND and stamps ModifiedDate.
// This means a soft-delete also bumps ModifiedDate, which is the
// desired behaviour (a soft-delete IS a state change worth recording).
builder.Services.AddSingleton<SoftDeleteInterceptor>();
builder.Services.AddSingleton<AuditableInterceptor>();

// ---------- 2. EF Core / SQLite ----------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");

builder.Services.AddDbContext<AppDbContext>((sp, opts) =>
{
    opts.UseSqlite(connectionString);

    // Pull the singleton interceptors back out of the container and
    // attach them to this DbContext's configuration. Each scoped
    // DbContext shares the same singleton instances.
    opts.AddInterceptors(
        sp.GetRequiredService<SoftDeleteInterceptor>(),
        sp.GetRequiredService<AuditableInterceptor>());
});

// ---------- 3. Application services ----------
//
// All scoped: one instance per HTTP request. Repositories hold a
// reference to the (scoped) DbContext, so any longer lifetime would
// leak the context across requests.
builder.Services.AddScoped<IProviderRepository, ProviderRepository>();
builder.Services.AddScoped<ILicenseRepository,  LicenseRepository>();
builder.Services.AddScoped<IProviderService,    ProviderService>();
builder.Services.AddScoped<ILicenseService,     LicenseService>();
builder.Services.AddScoped<IDashboardService,   DashboardService>();

// ---------- 4. MVC ----------
builder.Services.AddControllersWithViews();

var app = builder.Build();

// ---------- 5. DB initialisation ----------
//
// SQLite requires the target directory to exist before it will create
// the database file. Make sure /Data is there, then apply schema and
// seed. The whole thing is idempotent - safe to run on every boot. We
// do it BEFORE starting the HTTP listener so traffic only hits a
// fully-prepared database.
Directory.CreateDirectory(Path.Combine(app.Environment.ContentRootPath, "Data"));

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbInitializer.InitializeAsync(db);
}

// ---------- 6. HTTP pipeline ----------
//
// Middleware order is intentional and follows ASP.NET Core defaults:
//   - Exception handler / HSTS in production
//   - HTTPS redirect
//   - Static files (served before routing for performance)
//   - Routing
//   - Authorization (no-op today; placeholder for future auth)
//   - Endpoint mapping
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

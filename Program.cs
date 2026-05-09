using Microsoft.EntityFrameworkCore;
using ProviderAssignmentStarter.Infrastructure.Data;
using ProviderAssignmentStarter.Infrastructure.Interceptors;
using ProviderAssignmentStarter.Infrastructure.Repositories;
using ProviderAssignmentStarter.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------- Cross-cutting interceptors ----------
// Registered as singletons - they're stateless. The DbContext picks them
// up via AddInterceptors() below.
builder.Services.AddSingleton<SoftDeleteInterceptor>();
builder.Services.AddSingleton<AuditableInterceptor>();

// ---------- EF Core / SQLite ----------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");

builder.Services.AddDbContext<AppDbContext>((sp, opts) =>
{
    opts.UseSqlite(connectionString);
    opts.AddInterceptors(
        sp.GetRequiredService<SoftDeleteInterceptor>(),
        sp.GetRequiredService<AuditableInterceptor>());
});

// ---------- Application services ----------
builder.Services.AddScoped<IProviderRepository, ProviderRepository>();
builder.Services.AddScoped<ILicenseRepository,  LicenseRepository>();
builder.Services.AddScoped<IProviderService,    ProviderService>();
builder.Services.AddScoped<ILicenseService,     LicenseService>();
builder.Services.AddScoped<IDashboardService,   DashboardService>();

// ---------- MVC ----------
builder.Services.AddControllersWithViews();

var app = builder.Build();

// ---------- DB initialization ----------
// SQLite requires the target directory to exist before it will create the
// database file. Make sure /Data is there, then apply schema and seed.
// Idempotent - safe to run on every boot.
Directory.CreateDirectory(Path.Combine(app.Environment.ContentRootPath, "Data"));

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbInitializer.InitializeAsync(db);
}

// ---------- HTTP pipeline ----------
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

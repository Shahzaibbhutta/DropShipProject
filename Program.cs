using DropShipProject.Models;
using DropShipProject.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add services to the container.
builder.Services.AddDbContext<DatabaseContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    options.EnableSensitiveDataLogging(builder.Environment.IsDevelopment());
});

// Register application services
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<AccountService>();
builder.Services.AddSingleton<IEmailService, EmailService>();
// Configure Identity with custom settings
builder.Services.AddIdentity<User, IdentityRole<int>>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<DatabaseContext>()
.AddDefaultTokenProviders()
.AddDefaultUI();

// Add controllers with views
builder.Services.AddControllersWithViews(options =>
{
    options.MaxModelValidationErrors = 50;
});
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});
// Add Razor Pages support
builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
// Configure endpoints
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller}/{action=Index}/{id?}");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

// Database seeding with improved error handling
try
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;

    var logger = services.GetRequiredService<ILogger<Program>>();
    var context = services.GetRequiredService<DatabaseContext>();
    var userManager = services.GetRequiredService<UserManager<User>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole<int>>>();

    logger.LogInformation("Starting database initialization...");

    // Apply pending migrations
    logger.LogInformation("Applying migrations...");
    await context.Database.MigrateAsync();
    logger.LogInformation("Migrations applied successfully.");

    // Seed roles
    await SeedRolesAsync(roleManager, logger);

    // Seed admin user
    await SeedAdminUserAsync(userManager, logger);

    logger.LogInformation("Database initialization completed successfully.");
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "An error occurred during database initialization");
    throw; // Re-throw to ensure we see the error during startup
}

app.Run();

// Helper methods for seeding
async Task SeedRolesAsync(RoleManager<IdentityRole<int>> roleManager, ILogger logger)
{
    string[] roles = { "Supplier", "DropShipper" };

    foreach (var role in roles)
    {
        logger.LogInformation("Checking if {role} role exists...", role);
        if (!await roleManager.RoleExistsAsync(role))
        {
            logger.LogInformation("Creating {role} role...", role);
            var result = await roleManager.CreateAsync(new IdentityRole<int>(role));

            if (result.Succeeded)
            {
                logger.LogInformation("{role} role created successfully.", role);
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                logger.LogError("Failed to create {role} role. Errors: {errors}", role, errors);
            }
        }
        else
        {
            logger.LogInformation("{role} role already exists.", role);
        }
    }
}

async Task SeedAdminUserAsync(UserManager<User> userManager, ILogger logger)
{
    const string adminEmail = "admin@example.com";
    const string adminPassword = "Admin@123";

    if (await userManager.FindByEmailAsync(adminEmail) == null)
    {
        logger.LogInformation("Creating admin user...");

        var admin = new User
        {
            UserName = adminEmail,
            Email = adminEmail,
            UserType = "Supplier",
            CompanyName = "Admin Supplier",
            ContactPerson = "Admin",
            Address = "Default Address", // Add this line
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(admin, adminPassword);

        if (result.Succeeded)
        {
            logger.LogInformation("Admin user created successfully.");

            // Assign role to admin
            var roleResult = await userManager.AddToRoleAsync(admin, "Supplier");
            if (roleResult.Succeeded)
            {
                logger.LogInformation("Successfully assigned Supplier role to admin.");
            }
            else
            {
                var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                logger.LogError("Failed to assign role to admin. Errors: {errors}", errors);
            }
        }
        else
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            logger.LogError("Failed to create admin user. Errors: {errors}", errors);
        }
    }
    else
    {
        logger.LogInformation("Admin user already exists.");
    }
}
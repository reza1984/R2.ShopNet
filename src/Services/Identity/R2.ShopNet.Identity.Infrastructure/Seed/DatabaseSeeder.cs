using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using R2.ShopNet.Identity.Domain.Entities;
using R2.ShopNet.Identity.Infrastructure.Persistence;

namespace R2.ShopNet.Identity.Infrastructure.Seed;

/// <summary>
/// Seeds the database with initial data including default admin user and roles.
/// Uses ASP.NET Core Identity UserManager and RoleManager for proper user creation.
/// </summary>
public class DatabaseSeeder
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ILogger<DatabaseSeeder> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    /// <summary>
    /// Seeds the database with initial data including roles and admin user.
    /// </summary>
    public async Task SeedAsync()
    {
        try
        {
            _logger.LogInformation("Starting database seeding...");

            await SeedRolesAsync();
            await SeedAdminUserAsync();

            _logger.LogInformation("Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while seeding database");
            throw;
        }
    }

    private async Task SeedRolesAsync()
    {
        var roles = new[]
        {
            new ApplicationRole("Admin", "System administrator with full access"),
            new ApplicationRole("User", "Standard user with basic access"),
            new ApplicationRole("Manager", "Manager with elevated privileges")
        };

        foreach (var role in roles)
        {
            if (!await _roleManager.RoleExistsAsync(role.Name!))
            {
                _logger.LogInformation("Creating role: {RoleName}", role.Name);
                var result = await _roleManager.CreateAsync(role);
                
                if (result.Succeeded)
                {
                    _logger.LogInformation("Role {RoleName} created successfully", role.Name);
                }
                else
                {
                    _logger.LogWarning("Failed to create role {RoleName}: {Errors}",
                        role.Name, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                _logger.LogInformation("Role {RoleName} already exists, skipping", role.Name);
            }
        }
    }

    private async Task SeedAdminUserAsync()
    {
        // Check if admin user already exists
        var adminEmail = "admin@shopnet.com";
        var existingAdmin = await _userManager.FindByEmailAsync(adminEmail);

        if (existingAdmin != null)
        {
            _logger.LogInformation("Admin user already exists, skipping creation");
            return;
        }

        _logger.LogInformation("Creating default admin user...");

        // Create admin user using ApplicationUser
        var adminUser = new ApplicationUser(
            email: adminEmail,
            firstName: "System",
            lastName: "Administrator"
        )
        {
            EmailConfirmed = true, // Auto-confirm email for admin
            PhoneNumberConfirmed = false,
            TwoFactorEnabled = false,
            IsActive = true,
            CreatedBy = "System",
            CreatedAt = DateTime.UtcNow
        };

        // Default admin password: Admin@123
        var result = await _userManager.CreateAsync(adminUser, "Admin@123");

        if (result.Succeeded)
        {
            _logger.LogInformation("Admin user created successfully");

            // Add admin to Admin role
            var roleResult = await _userManager.AddToRoleAsync(adminUser, "Admin");
            
            if (roleResult.Succeeded)
            {
                _logger.LogInformation("Admin role assigned to admin user");
            }
            else
            {
                _logger.LogWarning("Failed to assign Admin role: {Errors}",
                    string.Join(", ", roleResult.Errors.Select(e => e.Description)));
            }

            _logger.LogInformation(
                "✓ Default admin user created successfully.\n" +
                "  Email: {Email}\n" +
                "  Password: Admin@123\n" +
                "  ⚠️  IMPORTANT: Change this password after first login!",
                adminEmail
            );
        }
        else
        {
            _logger.LogError("Failed to create admin user: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }
}

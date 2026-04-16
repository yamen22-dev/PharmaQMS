using Microsoft.AspNetCore.Identity;
using PharmaQMS.API.Core;
using PharmaQMS.API.Models.Entities;

namespace PharmaQMS.API.Data;

public static class IdentitySeeder
{
    private sealed record SeedUserDefinition(string Role, string FirstName, string LastName, string Email);

    private static readonly SeedUserDefinition[] DefaultUsers =
    [
        new(RoleNames.QAManager, "QA", "Manager", "qa.manager@pharmaqms.local"),
        new(RoleNames.QCAnalyst, "QC", "Analyst", "qc.analyst@pharmaqms.local"),
        new(RoleNames.ProductionAnalyst, "Production", "Analyst", "production.analyst@pharmaqms.local"),
        new(RoleNames.WarehouseOperator, "Warehouse", "Operator", "warehouse.operator@pharmaqms.local"),
        new(RoleNames.Viewer, "System", "Viewer", "viewer@pharmaqms.local")
    ];

    public static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager, CancellationToken cancellationToken = default)
    {
        foreach (var roleName in RoleNames.All)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var roleResult = await roleManager.CreateAsync(new IdentityRole(roleName));
                if (!roleResult.Succeeded)
                {
                    throw new InvalidOperationException($"Failed seeding role '{roleName}': {FormatErrors(roleResult.Errors)}");
                }
            }
        }
    }

    public static async Task SeedDefaultUsersAsync(
        UserManager<AuthUser> userManager,
        string defaultPassword,
        CancellationToken cancellationToken = default)
    {
        foreach (var seedUser in DefaultUsers)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var user = await userManager.FindByEmailAsync(seedUser.Email);
            if (user is null)
            {
                user = new AuthUser
                {
                    FirstName = seedUser.FirstName,
                    LastName = seedUser.LastName,
                    UserName = seedUser.Email,
                    Email = seedUser.Email,
                    EmailConfirmed = true,
                    LockoutEnabled = true
                };

                var createResult = await userManager.CreateAsync(user, defaultPassword);
                if (!createResult.Succeeded)
                {
                    throw new InvalidOperationException($"Failed seeding user '{seedUser.Email}': {FormatErrors(createResult.Errors)}");
                }
            }

            var currentRoles = await userManager.GetRolesAsync(user);

            foreach (var role in currentRoles.Where(role => role != seedUser.Role))
            {
                var removeResult = await userManager.RemoveFromRoleAsync(user, role);
                if (!removeResult.Succeeded)
                {
                    throw new InvalidOperationException($"Failed removing role '{role}' from '{seedUser.Email}': {FormatErrors(removeResult.Errors)}");
                }
            }

            if (!currentRoles.Contains(seedUser.Role))
            {
                var addRoleResult = await userManager.AddToRoleAsync(user, seedUser.Role);
                if (!addRoleResult.Succeeded)
                {
                    throw new InvalidOperationException($"Failed assigning role '{seedUser.Role}' to '{seedUser.Email}': {FormatErrors(addRoleResult.Errors)}");
                }
            }
        }
    }

    private static string FormatErrors(IEnumerable<IdentityError> errors)
        => string.Join(" ", errors.Select(error => error.Description));
}

using Microsoft.AspNetCore.Identity;
using Portfolio.Api.Data;

namespace Portfolio.Api.Services;

public class RoleService
{
    public static readonly string[] Names = ["Gast", "Editor", "Admin"];

    public static async Task EnsureRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        foreach (var name in Names)
        {
            if (!await roleManager.RoleExistsAsync(name))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(name));
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(string.Join("; ", result.Errors.Select(error => error.Description)));
                }
            }
        }
    }
    public static async Task AssignRoleAsync(UserManager<IdentityUser> userManager,
        PortfolioDbContext dbContext, string email, string role)
    {
        if (!Names.Contains(role))
        {
            throw new ArgumentException("Onbekende rol.", nameof(role));
        }

        var user = await userManager.FindByEmailAsync(email)
            ?? throw new InvalidOperationException("Gebruiker niet gevonden. Registreer eerst via de API.");
        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        EnsureSucceeded(await userManager.RemoveFromRolesAsync(user, await userManager.GetRolesAsync(user)));
        EnsureSucceeded(await userManager.AddToRoleAsync(user, role));
        EnsureSucceeded(await userManager.UpdateSecurityStampAsync(user));
        await transaction.CommitAsync();
    }

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(error => error.Description)));
        }
    }
}

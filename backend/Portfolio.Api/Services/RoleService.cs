using Microsoft.AspNetCore.Identity;

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
}

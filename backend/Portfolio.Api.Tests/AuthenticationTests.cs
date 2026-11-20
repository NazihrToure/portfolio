using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Portfolio.Api.Tests;

public class AuthenticationTests
{
    private static object Credentials(string email, string password) => new { email, password };
    private static string NewPassword() => $"{Guid.NewGuid():N}aA1!";

    [Fact]
    public async Task RegistrationHashesPasswordAndOnlyAssignsGast()
    {
        using var factory = new ApiFactory();
        using var client = factory.NewClient();
        await ApiFactory.RefreshCsrfAsync(client);
        var password = NewPassword();
        var response = await client.PostAsJsonAsync("/api/auth/register",
            new { email = "member@example.test", password, role = "Admin", roles = new[] { "Admin" } });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("member@example.test", body.GetProperty("email").GetString());
        Assert.Equal("Gast", Assert.Single(body.GetProperty("roles").EnumerateArray()).GetString());
        Assert.False(body.TryGetProperty("password", out _));
        Assert.False(body.TryGetProperty("passwordHash", out _));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
        await factory.WithDatabaseAsync(async db =>
        {
            var user = await db.Users.SingleAsync();
            Assert.NotEqual(password, user.PasswordHash);
            Assert.Equal(PasswordVerificationResult.Success,
                new PasswordHasher<IdentityUser>().VerifyHashedPassword(user, user.PasswordHash!, password));
            var membership = await db.UserRoles.SingleAsync();
            Assert.Equal("Gast", (await db.Roles.SingleAsync(role => role.Id == membership.RoleId)).Name);
            Assert.Equal(3, await db.Roles.CountAsync());
        });
        var duplicate = await client.PostAsJsonAsync("/api/auth/register", Credentials("MEMBER@example.test", password));
        Assert.Equal(HttpStatusCode.BadRequest, duplicate.StatusCode);
        await factory.WithDatabaseAsync(async db => Assert.Single(await db.Users.ToListAsync()));
    }

    [Theory]
    [InlineData("invalid", false)]
    [InlineData("", false)]
    [InlineData("member@example.test", true)]
    public async Task InvalidRegistrationDoesNotCreateUser(string email, bool weakPassword)
    {
        using var factory = new ApiFactory();
        using var client = factory.NewClient();
        await ApiFactory.RefreshCsrfAsync(client);
        var response = await client.PostAsJsonAsync("/api/auth/register", Credentials(email, weakPassword ? "short" : NewPassword()));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True((await response.Content.ReadFromJsonAsync<JsonElement>()).TryGetProperty("errors", out _));
        await factory.WithDatabaseAsync(async db => Assert.Empty(await db.Users.ToListAsync()));
    }

    [Fact]
    public async Task LoginUsesCookieAndLogoutRemovesAccess()
    {
        using var factory = new ApiFactory();
        using var client = factory.NewClient();
        await ApiFactory.RefreshCsrfAsync(client);
        var credentials = Credentials("member@example.test", NewPassword());
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/auth/register", credentials)).StatusCode);
        var login = await client.PostAsJsonAsync("/api/auth/login", credentials);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var cookie = Assert.Single(login.Headers.GetValues("Set-Cookie"), value => value.StartsWith(".AspNetCore.Identity.Application="));
        Assert.Contains("httponly", cookie.ToLowerInvariant());
        Assert.Contains("samesite=strict", cookie.ToLowerInvariant());
        Assert.Contains("secure", cookie.ToLowerInvariant());
        var current = await client.GetFromJsonAsync<JsonElement>("/api/auth/me");
        Assert.Equal("member@example.test", current.GetProperty("email").GetString());
        using var otherClient = factory.NewClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await otherClient.GetAsync("/api/auth/me")).StatusCode);
        await ApiFactory.RefreshCsrfAsync(client);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync("/api/auth/logout", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
    }

    [Fact]
    public async Task InvalidLoginIsRejectedAndRepeatedFailuresLockAccount()
    {
        using var factory = new ApiFactory();
        using var client = factory.NewClient();
        await ApiFactory.RefreshCsrfAsync(client);
        var password = NewPassword();
        await client.PostAsJsonAsync("/api/auth/register", Credentials("member@example.test", password));
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.PostAsJsonAsync("/api/auth/login", Credentials("missing@example.test", password))).StatusCode);
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var response = await client.PostAsJsonAsync("/api/auth/login", Credentials("member@example.test", NewPassword()));
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.False(response.Headers.Contains("Set-Cookie"));
        }
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.PostAsJsonAsync("/api/auth/login", Credentials("member@example.test", password))).StatusCode);
        await factory.WithDatabaseAsync(async db => Assert.True((await db.Users.SingleAsync()).LockoutEnd > DateTimeOffset.UtcNow));
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.PostAsJsonAsync("/api/auth/login", new { email = "invalid" })).StatusCode);
    }

    [Theory]
    [InlineData("register")]
    [InlineData("login")]
    public async Task AuthenticationRequiresCsrfToken(string action)
    {
        using var factory = new ApiFactory();
        using var client = factory.NewClient();
        var credentials = Credentials("member@example.test", NewPassword());
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync($"/api/auth/{action}", credentials)).StatusCode);
        await ApiFactory.RefreshCsrfAsync(client);
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", "invalid");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync($"/api/auth/{action}", credentials)).StatusCode);
        await factory.WithDatabaseAsync(async db => Assert.Empty(await db.Users.ToListAsync()));
    }
}

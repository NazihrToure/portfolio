using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Portfolio.Api.Tests;

public class AuthorizationTests
{
    [Theory]
    [InlineData("projects", null, HttpStatusCode.Unauthorized)]
    [InlineData("blogposts", null, HttpStatusCode.Unauthorized)]
    [InlineData("projects", "Gast", HttpStatusCode.Forbidden)]
    [InlineData("blogposts", "Gast", HttpStatusCode.Forbidden)]
    public async Task ForbiddenWritesDoNotChangeData(string resource, string? role, HttpStatusCode expected)
    {
        using var factory = new ApiFactory();
        using var editor = await factory.LoginAsync("Editor");
        var path = $"/api/{resource}";
        var created = await editor.PostAsJsonAsync(path, CrudTests.Payload(resource));
        var body = await created.Content.ReadFromJsonAsync<JsonElement>();
        var id = body.GetProperty("id").GetInt32();
        using var client = role is null ? factory.NewClient() : await factory.LoginAsync(role);
        foreach (var withCsrf in new[] { false, true })
        {
            client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
            if (withCsrf)
            {
                await ApiFactory.RefreshCsrfAsync(client);
            }

            Assert.Equal(expected, (await client.PostAsJsonAsync(path, CrudTests.Payload(resource))).StatusCode);
            Assert.Equal(expected, (await client.PutAsJsonAsync($"{path}/{id}", CrudTests.Payload(resource, "Gewijzigd"))).StatusCode);
            Assert.Equal(expected, (await client.DeleteAsync($"{path}/{id}")).StatusCode);
        }

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(path)).StatusCode);
        var fetched = await client.GetFromJsonAsync<JsonElement>($"{path}/{id}");
        Assert.Equal("Portfolio", fetched.GetProperty("title").GetString());
        await factory.WithDatabaseAsync(async db =>
        {
            if (resource == "projects")
            {
                Assert.Equal("Portfolio", (await db.Projects.SingleAsync()).Title);
            }
            else
            {
                Assert.Equal("Portfolio", (await db.Blogposts.SingleAsync()).Title);
            }
        });
    }

    [Theory]
    [InlineData("projects", "Editor")]
    [InlineData("projects", "Admin")]
    [InlineData("blogposts", "Editor")]
    [InlineData("blogposts", "Admin")]
    public async Task AuthenticatedRolesCanReadCollectionsAndItems(string resource, string role)
    {
        using var factory = new ApiFactory();
        using var client = await factory.LoginAsync(role);
        var path = $"/api/{resource}";
        var created = await client.PostAsJsonAsync(path, CrudTests.Payload(resource));
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(created.Headers.Location)).StatusCode);
    }

    [Theory]
    [InlineData("projects")]
    [InlineData("blogposts")]
    public async Task AuthorizedWritesRequireCsrfToken(string resource)
    {
        using var factory = new ApiFactory();
        using var client = await factory.LoginAsync("Editor");
        var path = $"/api/{resource}";
        var created = await client.PostAsJsonAsync(path, CrudTests.Payload(resource));
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(path, CrudTests.Payload(resource))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync(created.Headers.Location, CrudTests.Payload(resource))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.DeleteAsync(created.Headers.Location)).StatusCode);
        Assert.Single((await client.GetFromJsonAsync<JsonElement>(path)).EnumerateArray());
    }

    [Fact]
    public async Task RoleChangeInvalidatesExistingCookie()
    {
        using var factory = new ApiFactory();
        using var client = await factory.LoginAsync("Admin");
        var user = await client.GetFromJsonAsync<JsonElement>("/api/auth/me");
        await factory.AssignRoleAsync(user.GetProperty("email").GetString()!, "Gast");
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.PostAsJsonAsync("/api/projects", CrudTests.Payload("projects"))).StatusCode);
    }

    [Fact]
    public async Task RoleAssignmentIsRepeatableAndRejectsUnknownRoles()
    {
        using var factory = new ApiFactory();
        using var client = await factory.LoginAsync("Gast");
        var user = await client.GetFromJsonAsync<JsonElement>("/api/auth/me");
        var email = user.GetProperty("email").GetString()!;
        await factory.AssignRoleAsync(email, "Editor");
        await factory.AssignRoleAsync(email, "Editor");
        await Assert.ThrowsAsync<ArgumentException>(() => factory.AssignRoleAsync(email, "Owner"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => factory.AssignRoleAsync("missing@example.test", "Admin"));
        await factory.WithDatabaseAsync(async db =>
        {
            var assigned = await db.UserRoles.SingleAsync();
            Assert.Equal("Editor", (await db.Roles.SingleAsync(role => role.Id == assigned.RoleId)).Name);
        });
    }
}

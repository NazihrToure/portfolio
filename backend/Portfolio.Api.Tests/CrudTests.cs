using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Portfolio.Api.Tests;

public class CrudTests
{
    public static object Payload(string resource, string title = "Portfolio") => resource == "projects"
        ? new { title, description = "Beschrijving", category = "Web", technologies = new[] { "C#", "SQLite" },
            gitHubUrl = "https://github.com/example/portfolio", liveUrl = "https://example.com", date = "2026-11-01T00:00:00" }
        : (object)new { title, category = "Web", summary = "Samenvatting", content = "Inhoud", publicationDate = "2026-11-01T00:00:00" };

    [Theory]
    [InlineData("projects", "Editor")]
    [InlineData("projects", "Admin")]
    [InlineData("blogposts", "Editor")]
    [InlineData("blogposts", "Admin")]
    public async Task CrudPersistsAndReturnsExpectedData(string resource, string role)
    {
        using var factory = new ApiFactory();
        using var client = await factory.LoginAsync(role);
        using var anonymous = factory.NewClient();
        var path = $"/api/{resource}";
        Assert.Empty((await anonymous.GetFromJsonAsync<JsonElement>(path)).EnumerateArray());
        var created = await client.PostAsJsonAsync(path, Payload(resource));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var body = await created.Content.ReadFromJsonAsync<JsonElement>();
        var id = body.GetProperty("id").GetInt32();
        Assert.True(id > 0);
        Assert.Equal("Portfolio", body.GetProperty("title").GetString());
        Assert.EndsWith($"{path}/{id}", created.Headers.Location!.ToString());
        var fetched = await anonymous.GetFromJsonAsync<JsonElement>(created.Headers.Location);
        Assert.Equal(body.ToString(), fetched.ToString());
        Assert.Single((await anonymous.GetFromJsonAsync<JsonElement>(path)).EnumerateArray());
        await factory.WithDatabaseAsync(async db =>
        {
            if (resource == "projects")
            {
                var project = await db.Projects.SingleAsync();
                Assert.Equal(new[] { "C#", "SQLite" }, project.Technologies);
                Assert.Equal("https://example.com", project.LiveUrl);
            }
            else
            {
                Assert.Equal("Inhoud", (await db.Blogposts.SingleAsync()).Content);
            }
        });
        var updated = await client.PutAsJsonAsync($"{path}/{id}", Payload(resource, "Bijgewerkt"));
        Assert.Equal(HttpStatusCode.NoContent, updated.StatusCode);
        fetched = await anonymous.GetFromJsonAsync<JsonElement>($"{path}/{id}");
        Assert.Equal("Bijgewerkt", fetched.GetProperty("title").GetString());
        await factory.WithDatabaseAsync(async db =>
            Assert.Equal("Bijgewerkt", resource == "projects"
                ? (await db.Projects.SingleAsync()).Title : (await db.Blogposts.SingleAsync()).Title));
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"{path}/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync($"{path}/{id}")).StatusCode);
        Assert.Empty((await anonymous.GetFromJsonAsync<JsonElement>(path)).EnumerateArray());
        await factory.WithDatabaseAsync(async db =>
        {
            Assert.Empty(await db.Projects.ToListAsync());
            Assert.Empty(await db.Blogposts.ToListAsync());
        });
    }

    [Theory]
    [InlineData("projects")]
    [InlineData("blogposts")]
    public async Task MissingIdsReturnNotFound(string resource)
    {
        using var factory = new ApiFactory();
        using var client = await factory.LoginAsync("Editor");
        var path = $"/api/{resource}/999";
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync(path, Payload(resource))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync(path)).StatusCode);
    }

    [Theory]
    [InlineData("projects")]
    [InlineData("blogposts")]
    public async Task InvalidInputDoesNotChangeDatabase(string resource)
    {
        using var factory = new ApiFactory();
        using var client = await factory.LoginAsync("Editor");
        var path = $"/api/{resource}";
        var invalid = await client.PostAsJsonAsync(path, new { title = "" });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        var errors = await invalid.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(errors.GetProperty("errors").TryGetProperty("Title", out _));
        var created = await client.PostAsJsonAsync(path, Payload(resource));
        var body = await created.Content.ReadFromJsonAsync<JsonElement>();
        var id = body.GetProperty("id").GetInt32();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync($"{path}/{id}", new { title = "" })).StatusCode);
        var fetched = await client.GetFromJsonAsync<JsonElement>($"{path}/{id}");
        Assert.Equal("Portfolio", fetched.GetProperty("title").GetString());
        Assert.Single((await client.GetFromJsonAsync<JsonElement>(path)).EnumerateArray());
    }

    [Fact]
    public async Task InvalidProjectUrlAndNullTechnologiesReturnBadRequest()
    {
        using var factory = new ApiFactory();
        using var client = await factory.LoginAsync("Editor");
        foreach (var payload in new object[]
        {
            new { title = "Test", description = "Test", category = "Web", gitHubUrl = "invalid" },
            new { title = "Test", description = "Test", category = "Web", technologies = (string[]?)null }
        })
        {
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/projects", payload)).StatusCode);
        }
        await factory.WithDatabaseAsync(async db => Assert.Empty(await db.Projects.ToListAsync()));
    }
}

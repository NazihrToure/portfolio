using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Portfolio.Api.Data;
using Portfolio.Api.Services;

namespace Portfolio.Api.Tests;

public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public ApiFactory()
    {
        _connection.Open();
        using var db = new PortfolioDbContext(new DbContextOptionsBuilder<PortfolioDbContext>()
            .UseSqlite(_connection).Options);
        db.Database.Migrate();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<PortfolioDbContext>();
            services.RemoveAll<SqlServerPortfolioDbContext>();
            services.RemoveAll<DbContextOptions<SqlServerPortfolioDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<SqlServerPortfolioDbContext>>();
            services.RemoveAll<DbContextOptions<PortfolioDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<PortfolioDbContext>>();
            services.AddDbContext<PortfolioDbContext>(options => options.UseSqlite(_connection));
        });
    }

    public HttpClient NewClient() => CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        HandleCookies = true,
        BaseAddress = new Uri("https://localhost")
    });

    public static async Task RefreshCsrfAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/auth/csrf");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", body.GetProperty("token").GetString());
    }

    public async Task<HttpClient> LoginAsync(string role)
    {
        var client = NewClient();
        await RefreshCsrfAsync(client);
        var email = $"{Guid.NewGuid():N}@example.test";
        var password = $"{Guid.NewGuid():N}aA1!";
        var response = await client.PostAsJsonAsync("/api/auth/register", new { email, password });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        await AssignRoleAsync(email, role);
        response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await RefreshCsrfAsync(client);
        return client;
    }

    public async Task AssignRoleAsync(string email, string role)
    {
        using var scope = Services.CreateScope();
        await RoleService.AssignRoleAsync(scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>(),
            scope.ServiceProvider.GetRequiredService<PortfolioDbContext>(), email, role);
    }

    public async Task WithDatabaseAsync(Func<PortfolioDbContext, Task> action)
    {
        using var scope = Services.CreateScope();
        await action(scope.ServiceProvider.GetRequiredService<PortfolioDbContext>());
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }
}

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Portfolio.Api.Data;
using Portfolio.Api.Models;

namespace Portfolio.Api.Tests;

public class MigrationTests
{
    [Fact]
    public async Task IdentityMigrationPreservesExistingPortfolioData()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new PortfolioDbContext(new DbContextOptionsBuilder<PortfolioDbContext>()
            .UseSqlite(connection).Options);
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync("20261008112325_MakeProjectGitHubUrlOptional");
        db.Projects.Add(new Project { Title = "Bestaand project", Technologies = ["C#"] });
        db.Blogposts.Add(new Blogpost { Title = "Bestaande blogpost", Content = "Bewaarde inhoud" });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        await migrator.MigrateAsync();
        Assert.Equal("Bestaand project", (await db.Projects.SingleAsync()).Title);
        Assert.Equal(new[] { "C#" }, (await db.Projects.SingleAsync()).Technologies);
        Assert.Equal("Bewaarde inhoud", (await db.Blogposts.SingleAsync()).Content);
        Assert.Empty(await db.Users.ToListAsync());
        Assert.False(db.Database.HasPendingModelChanges());
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
    }
}

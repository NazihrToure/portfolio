using Microsoft.EntityFrameworkCore;

namespace Portfolio.Api.Data;

public class SqlServerPortfolioDbContext : PortfolioDbContext
{
    public SqlServerPortfolioDbContext(DbContextOptions<SqlServerPortfolioDbContext> options) : base(options)
    {
    }
}

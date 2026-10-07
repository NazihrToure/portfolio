using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Portfolio.Api.Data;

var builder = WebApplication.CreateBuilder(args);

var connectionString = new SqliteConnectionStringBuilder(
    builder.Configuration.GetConnectionString("PortfolioDatabase"));
connectionString.DataSource = Path.GetFullPath(
    connectionString.DataSource, builder.Environment.ContentRootPath);

builder.Services.AddDbContext<PortfolioDbContext>(options =>
    options.UseSqlite(connectionString.ConnectionString));

builder.Services.AddControllers();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();

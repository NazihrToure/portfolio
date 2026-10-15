using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Portfolio.Api.Data;
using Portfolio.Api.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = new SqliteConnectionStringBuilder(
    builder.Configuration.GetConnectionString("PortfolioDatabase"));
connectionString.DataSource = Path.GetFullPath(
    connectionString.DataSource, builder.Environment.ContentRootPath);

builder.Services.AddDbContext<PortfolioDbContext>(options =>
    options.UseSqlite(connectionString.ConnectionString));

builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("PortfolioFrontend", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? Array.Empty<string>();
        policy.WithOrigins(origins).WithMethods("GET");
    });
});
builder.Services.AddScoped<ProjectService>();
builder.Services.AddScoped<BlogpostService>();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("PortfolioFrontend");
app.MapControllers();

app.Run();

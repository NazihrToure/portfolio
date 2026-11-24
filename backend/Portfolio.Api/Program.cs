using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Portfolio.Api.Data;
using Portfolio.Api.Services;

var assigningRole = args.FirstOrDefault() == "assign-role";
var builder = WebApplication.CreateBuilder(assigningRole ? Array.Empty<string>() : args);

builder.Services.AddDbContext<PortfolioDbContext, SqlServerPortfolioDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("PortfolioDatabase")));

builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.Password.RequiredLength = 10;
    options.Lockout.MaxFailedAccessAttempts = 5;
})
    .AddEntityFrameworkStores<PortfolioDbContext>()
    .AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
    options.ValidationInterval = TimeSpan.Zero);
builder.Services.AddAuthorization();
builder.Services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");
builder.Services.AddControllersWithViews(options =>
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute()));
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

using (var scope = app.Services.CreateScope())
{
    await RoleService.EnsureRolesAsync(scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>());
    if (assigningRole)
    {
        if (!app.Environment.IsDevelopment() || args.Length != 3 || !RoleService.Names.Contains(args[2]))
        {
            throw new InvalidOperationException("Gebruik in Development: assign-role <email> <Gast|Editor|Admin>");
        }

        await RoleService.AssignRoleAsync(scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>(),
            scope.ServiceProvider.GetRequiredService<PortfolioDbContext>(), args[1], args[2]);
        Console.WriteLine($"Rol {args[2]} toegekend aan {args[1]}. Log opnieuw in.");
        return;
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("PortfolioFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }

using loyalty_sfotware.Components;
using gussmann_loyalty_program.Services;
using Microsoft.EntityFrameworkCore;
using gussmann_loyalty_program.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using gussmann_loyalty_program.Models;
using Microsoft.Extensions.Logging;
using BCrypt.Net;
using Microsoft.AspNetCore.Builder;
// StaticWebAssetsLoader removed: Blazor Server automatically serves package/static assets via MapStaticAssets.

var builder = WebApplication.CreateBuilder(args);
// Register Radzen services for dialogs, notifications, tooltips, context menus
builder.Services.AddScoped<Radzen.DialogService>();
builder.Services.AddScoped<Radzen.NotificationService>();
builder.Services.AddScoped<Radzen.TooltipService>();
builder.Services.AddScoped<Radzen.ContextMenuService>();

// (Static web assets loader removed: API not available in current target. Will rely on MapStaticAssets.)

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add controllers and DB context (do not run migrations automatically)
builder.Services.AddControllers();

// Configure DbContext: prefer configured DefaultConnection (SQL Server),
// but when no connection string is present (common on developer machines),
// fall back to a local SQLite file so we can run and test authentication locally.
var defaultConn = builder.Configuration.GetConnectionString("DefaultConnection");
var useSqliteDevFallback = string.IsNullOrWhiteSpace(defaultConn);
if (!useSqliteDevFallback)
{
    builder.Services.AddDbContext<NewLoyaltyDbContext>(options =>
        options.UseSqlServer(defaultConn));
}
else
{
    // Developer fallback: use an in-memory EF Core database for local testing.
    // This avoids SQL dialect differences and doesn't require an external DB.
    builder.Services.AddDbContext<NewLoyaltyDbContext>(options =>
        options.UseInMemoryDatabase("dev_loyalty_db"));
}

// Register LoyaltyDbContext (used by UI pages) with the same provider strategy.
if (!useSqliteDevFallback)
{
    builder.Services.AddDbContext<LoyaltyDbContext>(options =>
        options.UseSqlServer(defaultConn));
}
else
{
    builder.Services.AddDbContext<LoyaltyDbContext>(options =>
        options.UseInMemoryDatabase("dev_loyalty_seed_db"));
}

// Configure JWT authentication (settings come from configuration)
var jwtSection = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSection["SecretKey"] ?? "GussmannLoyaltySecretKey2024ForJWTAuthentication!@#$%";
var issuer = jwtSection["Issuer"] ?? "GussmannLoyaltyProgram";
var audience = jwtSection["Audience"] ?? "GussmannLoyaltyUsers";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secretKey)),
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

// Register authentication service used by controllers (use local merged project's service)
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
// Register domain services used by pages
builder.Services.AddScoped<LoyaltyService>();

    // Register front-end auth service with an HttpClient for calling the API
    builder.Services.AddHttpClient<gussmann_loyalty_program.Services.IAuthService, gussmann_loyalty_program.Services.AuthService>(client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7001");
    });

var app = builder.Build();

// Static web assets are mapped by app.MapStaticAssets(); no manual loader call required.

// Apply migrations & seed admin user if none exists (works for MSSQL or fallback provider)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var db = services.GetRequiredService<NewLoyaltyDbContext>();
        // Apply migrations automatically when using SQL Server (skip for in-memory)
        if (!useSqliteDevFallback)
        {
            db.Database.Migrate();
        }
        if (!db.Admins.Any())
        {
            var passwordHash = BCrypt.Net.BCrypt.HashPassword("admin123");
            var admin = new gussmann_loyalty_program.Models.Admin
            {
                Username = "principaltest",
                Email = "principaltest",
                PasswordHash = passwordHash,
                FullName = "Principal Test",
                Role = "Admin",
                IsActive = true
            };
            db.Admins.Add(admin);
            db.SaveChanges();
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Seeded admin 'principaltest' (password 'admin123'). Provider: {Provider}", useSqliteDevFallback ? "InMemory" : "SqlServer");
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating/seeding the database.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();

}

// Only enable https redirection in non-development environments (avoids missing https port in dev)
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Single antiforgery registration (remove duplicate)
app.UseAntiforgery();

app.MapControllers();
app.MapStaticAssets();
app.MapRazorComponents<global::loyalty_sfotware.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();

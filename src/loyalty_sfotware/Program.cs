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

var builder = WebApplication.CreateBuilder(args);

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

// If we are using the in-memory dev fallback, seed a known test admin so login
// can be tested without an external DB. This runs only in Development.
if (useSqliteDevFallback && app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    try
    {
        var db = services.GetRequiredService<NewLoyaltyDbContext>();

        // Seed a test admin if none exists. Username: principaltest, password: admin123
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
            logger.LogInformation("Seeded development admin 'principaltest' with password 'admin123' into in-memory DB.");
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the development in-memory DB.");
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

app.UseAntiforgery();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Anti-forgery middleware must be registered after authentication/authorization
// and before endpoint mappings (MapControllers/MapRazorComponents).
app.UseAntiforgery();

app.MapControllers();
app.MapStaticAssets();
app.MapRazorComponents<global::loyalty_sfotware.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();

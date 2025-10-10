using gussmann_loyalty_program.Components;
using gussmann_loyalty_program.Data;
using gussmann_loyalty_program.Services;
using gussmann_loyalty_program.Extensions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add MVC services for the login controller
builder.Services.AddControllersWithViews();

// Add controllers for API endpoints
builder.Services.AddControllers();

// Add Entity Framework
builder.Services.AddDbContext<NewLoyaltyDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add JWT Authentication
builder.Services.AddJwtAuthentication(builder.Configuration);

// Add application services
builder.Services.AddScoped<NewLoyaltyService>();
builder.Services.AddScoped<ISimpleAuthService, SimpleAuthService>();

// Add HTTP Client for API calls
builder.Services.AddHttpClient();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

// Add authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();

// Map Blazor components FIRST (higher priority)
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Map API controllers
app.MapControllers();

// Map SPECIFIC MVC routes only (not catch-all)
app.MapControllerRoute(
    name: "login",
    pattern: "login/{action=Index}",
    defaults: new { controller = "Login" });

app.MapControllerRoute(
    name: "root",
    pattern: "",
    defaults: new { controller = "Login", action = "Index" });

app.Run();

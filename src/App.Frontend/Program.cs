using gussmann_loyalty_program.Components;
using gussmann_loyalty_program.Data;
using gussmann_loyalty_program.Services;
using App.Common.Extensions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();


// Add Entity Framework
builder.Services.AddDbContext<NewLoyaltyDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("App.Database")));

// Add JWT Authentication
builder.Services.AddJwtAuthentication(builder.Configuration);

// Add application services
builder.Services.AddScoped<NewLoyaltyService>();
builder.Services.AddScoped<IAuthService, AuthService>();

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
app.MapRazorComponents<gussmann_loyalty_program.Components.App>()
    .AddInteractiveServerRenderMode();


app.Run();

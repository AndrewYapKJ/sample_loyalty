#r "nuget: Microsoft.EntityFrameworkCore.SqlServer, 9.0.9"
#r "nuget: Microsoft.Extensions.Configuration.Json, 9.0.0"
#r "nuget: Microsoft.AspNetCore.Cryptography.KeyDerivation, 9.0.0"
#load "../App.Model/Models/Admin.cs"
#load "../App.Database/Data/NewLoyaltyDbContext.cs"

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Cryptography;
using gussmann_loyalty_program.Data;
using gussmann_loyalty_program.Models;

// Build configuration
var configuration = new ConfigurationBuilder()
    .SetBasePath("../App.Frontend")
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile("appsettings.Development.json", optional: true)
    .Build();

// Get connection string  
var connectionString = configuration.GetConnectionString("DefaultConnection");

// Configure DbContext
var optionsBuilder = new DbContextOptionsBuilder<NewLoyaltyDbContext>();
optionsBuilder.UseSqlServer(connectionString);

using var context = new NewLoyaltyDbContext(optionsBuilder.Options);

// Hash password function (same as SimpleAuthService)
string HashPassword(string password)
{
    byte[] salt = new byte[128 / 8];
    using (var rng = RandomNumberGenerator.Create())
    {
        rng.GetBytes(salt);
    }

    string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
        password: password,
        salt: salt,
        prf: KeyDerivationPrf.HMACSHA256,
        iterationCount: 100000,
        numBytesRequested: 256 / 8));

    return $"{Convert.ToBase64String(salt)}.{hashed}";
}

// Check if admin already exists
var existingAdmin = await context.Admins
    .FirstOrDefaultAsync(a => a.Username == "principaltest" || a.Email == "principaltest");

if (existingAdmin != null)
{
    Console.WriteLine("❌ Admin user already exists.");
    return;
}

// Create admin user
var admin = new Admin
{
    Id = Guid.NewGuid().ToString(),
    Username = "principaltest",
    Email = "principaltest", 
    FullName = "Principal Administrator",
    Role = "principal",
    PasswordHash = HashPassword("princialtest"),
    IsActive = true,
    CreatedAt = DateTime.UtcNow
};

context.Admins.Add(admin);
await context.SaveChangesAsync();

Console.WriteLine("✅ Admin user created successfully!");
Console.WriteLine($"Username: {admin.Username}");
Console.WriteLine($"Email: {admin.Email}");
Console.WriteLine($"Role: {admin.Role}");

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace gussmann_loyalty_program.Extensions
{
    public static class AuthenticationExtensions
    {
        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var secretKey = configuration["Jwt:SecretKey"] ?? "GussmannLoyaltySecretKey2024ForJWTAuthentication!@#$%";
            var issuer = configuration["Jwt:Issuer"] ?? "GussmannLoyaltyProgram";
            var audience = configuration["Jwt:Audience"] ?? "GussmannLoyaltyUsers";

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                    ClockSkew = TimeSpan.Zero
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        // Check for token in Authorization header
                        var authorization = context.Request.Headers["Authorization"].FirstOrDefault();
                        if (!string.IsNullOrEmpty(authorization) && authorization.StartsWith("Bearer "))
                        {
                            context.Token = authorization["Bearer ".Length..].Trim();
                        }
                        
                        // Also check for token in cookies for Blazor server
                        if (string.IsNullOrEmpty(context.Token))
                        {
                            context.Token = context.Request.Cookies["AccessToken"];
                        }

                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        Console.WriteLine($"JWT Authentication failed: {context.Exception.Message}");
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        Console.WriteLine($"JWT Token validated for user: {context.Principal?.Identity?.Name}");
                        return Task.CompletedTask;
                    }
                };
            });

            services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminOnly", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireRole("Admin");
                });

                options.AddPolicy("SuperAdminOnly", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireRole("SuperAdmin");
                });
            });

            return services;
        }
    }
}
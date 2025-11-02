using gussmann_loyalty_program.Data;
using gussmann_loyalty_program.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using App.Api.DTOs;

namespace App.Api.Services
{
    public interface IAuthenticationService
    {
        Task<LoginResponse> LoginAsync(LoginRequest request);
        Task<LoginResponse?> RefreshTokenAsync(RefreshTokenRequest request);
        Task<ValidateTokenResponse> ValidateTokenAsync(ValidateTokenRequest request);
        Task<ClaimsPrincipal?> GetPrincipalFromTokenAsync(string token);
    }

    public class AuthenticationService : IAuthenticationService
    {
        private readonly NewLoyaltyDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthenticationService> _logger;
        private readonly string _secretKey;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _accessTokenExpiryMinutes;
        private readonly int _refreshTokenExpiryDays;

        public AuthenticationService(
            NewLoyaltyDbContext context,
            IConfiguration configuration,
            ILogger<AuthenticationService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;

            _secretKey = _configuration["Jwt:SecretKey"] ?? "GussmannLoyaltySecretKey2024ForJWTAuthentication!@#$%";
            _issuer = _configuration["Jwt:Issuer"] ?? "GussmannLoyaltyProgram";
            _audience = _configuration["Jwt:Audience"] ?? "GussmannLoyaltyUsers";
            _accessTokenExpiryMinutes = int.Parse(_configuration["Jwt:AccessTokenExpiryMinutes"] ?? "15");
            _refreshTokenExpiryDays = int.Parse(_configuration["Jwt:RefreshTokenExpiryDays"] ?? "30");
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            try
            {
                _logger.LogInformation("Login attempt for email: {Email}", request.Email);
                
                // Find admin by email or username
                var admin = await _context.Admins
                    .FirstOrDefaultAsync(a => a.Email == request.Email || a.Username == request.Email);
                
                _logger.LogInformation("Admin found: {Found}, IsActive: {IsActive}", admin != null, admin?.IsActive);

                if (admin == null || !admin.IsActive)
                {
                    _logger.LogWarning("Login attempt with invalid email: {Email}", request.Email);
                    return new LoginResponse { Success = false, Message = "Invalid email or password." };
                }

                // Check if account is locked
                if (admin.LockedUntil.HasValue && admin.LockedUntil > DateTime.UtcNow)
                {
                    _logger.LogWarning("Login attempt for locked account: {Email}", request.Email);
                    return new LoginResponse { Success = false, Message = "Account is temporarily locked." };
                }

                // Verify password using BCrypt
                _logger.LogInformation("Verifying password for email: {Email}, PasswordHash: {Hash}", request.Email, admin.PasswordHash);
                
                if (!BCrypt.Net.BCrypt.Verify(request.Password, admin.PasswordHash))
                {
                    _logger.LogWarning("Password verification failed for email: {Email}", request.Email);
                    admin.LoginAttempts++;
                    if (admin.LoginAttempts >= 5)
                    {
                        admin.LockedUntil = DateTime.UtcNow.AddMinutes(30);
                        _logger.LogWarning("Account locked due to too many failed attempts: {Email}", request.Email);
                    }
                    await _context.SaveChangesAsync();

                    return new LoginResponse { Success = false, Message = "Invalid email or password." };
                }
                
                _logger.LogInformation("Password verification successful for email: {Email}", request.Email);

                // Reset login attempts on successful login
                admin.LoginAttempts = 0;
                admin.LockedUntil = null;
                admin.LastLoginAt = DateTime.UtcNow;

                // Generate tokens
                var jwtId = Guid.NewGuid().ToString();
                var accessTokenExpiry = DateTime.UtcNow.AddMinutes(_accessTokenExpiryMinutes);
                var refreshTokenExpiry = DateTime.UtcNow.AddDays(_refreshTokenExpiryDays);

                var accessToken = GenerateAccessToken(admin, jwtId, accessTokenExpiry);
                var refreshToken = GenerateRefreshToken();

                // Store refresh token in database
                var tokenEntity = new RefreshToken
                {
                    Id = Guid.NewGuid().ToString(),
                    AdminId = admin.Id,
                    Token = refreshToken,
                    JwtId = jwtId,
                    IsUsed = false,
                    IsRevoked = false,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = refreshTokenExpiry
                };

                _context.RefreshTokens.Add(tokenEntity);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successful login for admin: {Email}", request.Email);

                return new LoginResponse
                {
                    Success = true,
                    Message = "Login successful.",
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    AccessTokenExpiry = accessTokenExpiry,
                    RefreshTokenExpiry = refreshTokenExpiry,
                    Admin = new AdminDto
                    {
                        Id = admin.Id,
                        Username = admin.Username,
                        Email = admin.Email,
                        FullName = admin.FullName,
                        Role = admin.Role,
                        IsActive = admin.IsActive,
                        CreatedAt = admin.CreatedAt,
                        LastLoginAt = admin.LastLoginAt
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for email: {Email}", request.Email);
                return new LoginResponse { Success = false, Message = "An error occurred during login." };
            }
        }

        public async Task<LoginResponse?> RefreshTokenAsync(RefreshTokenRequest request)
        {
            try
            {
                var storedToken = await _context.RefreshTokens
                    .Include(rt => rt.Admin)
                    .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);

                if (storedToken == null || storedToken.IsUsed || storedToken.IsRevoked || 
                    storedToken.ExpiresAt <= DateTime.UtcNow)
                {
                    return null;
                }

                var admin = storedToken.Admin;
                if (admin == null || !admin.IsActive)
                {
                    return null;
                }

                // Mark current token as used
                storedToken.IsUsed = true;

                // Generate new tokens
                var jwtId = Guid.NewGuid().ToString();
                var accessTokenExpiry = DateTime.UtcNow.AddMinutes(_accessTokenExpiryMinutes);
                var refreshTokenExpiry = DateTime.UtcNow.AddDays(_refreshTokenExpiryDays);

                var accessToken = GenerateAccessToken(admin, jwtId, accessTokenExpiry);
                var newRefreshToken = GenerateRefreshToken();

                // Store new refresh token
                var newTokenEntity = new RefreshToken
                {
                    Id = Guid.NewGuid().ToString(),
                    AdminId = admin.Id,
                    Token = newRefreshToken,
                    JwtId = jwtId,
                    IsUsed = false,
                    IsRevoked = false,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = refreshTokenExpiry
                };

                _context.RefreshTokens.Add(newTokenEntity);
                await _context.SaveChangesAsync();

                return new LoginResponse
                {
                    Success = true,
                    Message = "Token refreshed successfully.",
                    AccessToken = accessToken,
                    RefreshToken = newRefreshToken,
                    AccessTokenExpiry = accessTokenExpiry,
                    RefreshTokenExpiry = refreshTokenExpiry,
                    Admin = new AdminDto
                    {
                        Id = admin.Id,
                        Username = admin.Username,
                        Email = admin.Email,
                        FullName = admin.FullName,
                        Role = admin.Role,
                        IsActive = admin.IsActive,
                        CreatedAt = admin.CreatedAt,
                        LastLoginAt = admin.LastLoginAt
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                return null;
            }
        }

        public async Task<ValidateTokenResponse> ValidateTokenAsync(ValidateTokenRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Token))
                {
                    return new ValidateTokenResponse { IsValid = false };
                }

                var principal = await GetPrincipalFromTokenAsync(request.Token);
                if (principal == null)
                {
                    return new ValidateTokenResponse { IsValid = false };
                }

                var username = principal.FindFirst(ClaimTypes.Name)?.Value;
                var role = principal.FindFirst(ClaimTypes.Role)?.Value;

                return new ValidateTokenResponse
                {
                    IsValid = true,
                    Username = username,
                    Role = role
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating token");
                return new ValidateTokenResponse { IsValid = false };
            }
        }

        public async Task<ClaimsPrincipal?> GetPrincipalFromTokenAsync(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_secretKey);

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _issuer,
                    ValidateAudience = true,
                    ValidAudience = _audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
                return await Task.FromResult(principal);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Token validation failed");
                return null;
            }
        }

        private string GenerateAccessToken(Admin admin, string jwtId, DateTime expiry)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_secretKey);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, admin.Id),
                new(ClaimTypes.Name, admin.Username),
                new(ClaimTypes.Email, admin.Email),
                new(ClaimTypes.Role, admin.Role),
                new(JwtRegisteredClaimNames.Jti, jwtId),
                new(JwtRegisteredClaimNames.Iat, new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                new("full_name", admin.FullName)
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = expiry,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256),
                Issuer = _issuer,
                Audience = _audience
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private static string GenerateRefreshToken()
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }
    }
}
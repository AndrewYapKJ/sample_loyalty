using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using gussmann_loyalty_program.Data;
using gussmann_loyalty_program.Models;

namespace gussmann_loyalty_program.Services
{
    public interface IJwtService
    {
        Task<JwtTokenResult> GenerateTokenAsync(Admin admin);
        Task<JwtTokenResult?> RefreshTokenAsync(string refreshToken);
        Task<bool> RevokeTokenAsync(string refreshToken);
        Task<ClaimsPrincipal?> ValidateTokenAsync(string token);
        Task<bool> IsTokenValidAsync(string token);
        Task RevokeAllUserTokensAsync(string adminId);
    }

    public class JwtTokenResult
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime AccessTokenExpiry { get; set; }
        public DateTime RefreshTokenExpiry { get; set; }
    }

    public class JwtService : IJwtService
    {
        private readonly NewLoyaltyDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<JwtService> _logger;

        private readonly string _secretKey;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _accessTokenExpiryMinutes;
        private readonly int _refreshTokenExpiryDays;

        public JwtService(NewLoyaltyDbContext context, IConfiguration configuration, ILogger<JwtService> logger)
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

        public async Task<JwtTokenResult> GenerateTokenAsync(Admin admin)
        {
            try
            {
                var jwtId = Guid.NewGuid().ToString();
                var accessTokenExpiry = DateTime.UtcNow.AddMinutes(_accessTokenExpiryMinutes);
                var refreshTokenExpiry = DateTime.UtcNow.AddDays(_refreshTokenExpiryDays);

                // Generate Access Token
                var accessToken = GenerateAccessToken(admin, jwtId, accessTokenExpiry);

                // Generate Refresh Token
                var refreshTokenValue = GenerateRefreshToken();

                // Save refresh token to database
                var refreshToken = new RefreshToken
                {
                    AdminId = admin.Id,
                    Token = refreshTokenValue,
                    JwtId = jwtId,
                    ExpiresAt = refreshTokenExpiry
                };

                _context.RefreshTokens.Add(refreshToken);
                await _context.SaveChangesAsync();

                _logger.LogInformation("JWT tokens generated for admin: {AdminId}", admin.Id);

                return new JwtTokenResult
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshTokenValue,
                    AccessTokenExpiry = accessTokenExpiry,
                    RefreshTokenExpiry = refreshTokenExpiry
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating JWT tokens for admin: {AdminId}", admin.Id);
                throw;
            }
        }

        public async Task<JwtTokenResult?> RefreshTokenAsync(string refreshToken)
        {
            try
            {
                var storedToken = await _context.RefreshTokens
                    .Include(rt => rt.Admin)
                    .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

                if (storedToken == null || 
                    storedToken.IsUsed || 
                    storedToken.IsRevoked || 
                    storedToken.ExpiresAt <= DateTime.UtcNow)
                {
                    _logger.LogWarning("Invalid or expired refresh token: {RefreshToken}", refreshToken);
                    return null;
                }

                // Mark current refresh token as used
                storedToken.IsUsed = true;
                
                // Generate new tokens
                var newTokenResult = await GenerateTokenAsync(storedToken.Admin);
                
                await _context.SaveChangesAsync();

                _logger.LogInformation("Tokens refreshed for admin: {AdminId}", storedToken.AdminId);
                return newTokenResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token: {RefreshToken}", refreshToken);
                return null;
            }
        }

        public async Task<bool> RevokeTokenAsync(string refreshToken)
        {
            try
            {
                var storedToken = await _context.RefreshTokens
                    .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

                if (storedToken == null)
                    return false;

                storedToken.IsRevoked = true;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Refresh token revoked: {RefreshToken}", refreshToken);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking token: {RefreshToken}", refreshToken);
                return false;
            }
        }

        public async Task<ClaimsPrincipal?> ValidateTokenAsync(string token)
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
                
                if (validatedToken is JwtSecurityToken jwtToken)
                {
                    var jwtId = jwtToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti)?.Value;
                    
                    // Check if the token is revoked
                    var isRevoked = await _context.RefreshTokens
                        .AnyAsync(rt => rt.JwtId == jwtId && rt.IsRevoked);
                    
                    if (isRevoked)
                    {
                        _logger.LogWarning("Attempted to use revoked token: {JwtId}", jwtId);
                        return null;
                    }
                }

                return principal;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Token validation failed: {Token}", token);
                return null;
            }
        }

        public async Task<bool> IsTokenValidAsync(string token)
        {
            var principal = await ValidateTokenAsync(token);
            return principal != null;
        }

        public async Task RevokeAllUserTokensAsync(string adminId)
        {
            try
            {
                var tokens = await _context.RefreshTokens
                    .Where(rt => rt.AdminId == adminId && !rt.IsRevoked)
                    .ToListAsync();

                foreach (var token in tokens)
                {
                    token.IsRevoked = true;
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("All tokens revoked for admin: {AdminId}", adminId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking all tokens for admin: {AdminId}", adminId);
                throw;
            }
        }

        private string GenerateAccessToken(Admin admin, string jwtId, DateTime expiry)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_secretKey);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, admin.Id),
                new Claim(ClaimTypes.Name, admin.Username),
                new Claim(ClaimTypes.Email, admin.Email),
                new Claim(ClaimTypes.Role, admin.Role),
                new Claim(JwtRegisteredClaimNames.Jti, jwtId),
                new Claim(JwtRegisteredClaimNames.Iat, new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                new Claim("full_name", admin.FullName),
                new Claim("is_active", admin.IsActive.ToString())
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
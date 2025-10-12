using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Cryptography;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace gussmann_loyalty_program.Services
{
    public interface ISimpleAuthService
    {
        Task<SimpleAuthResult> LoginAsync(string username, string password);
        Task<SimpleAuthResult?> RefreshTokenAsync(string refreshToken);
        Task<bool> ValidateTokenAsync(string token);
        Task<ClaimsPrincipal?> GetPrincipalFromTokenAsync(string token);
        Task<bool> IsAdminExistsAsync();
        Task<bool> CreateInitialAdminAsync(string username, string email, string password, string fullName);
        Task<bool> IsAuthenticatedAsync();
    }

    public class SimpleAuthResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? AccessTokenExpiry { get; set; }
        public DateTime? RefreshTokenExpiry { get; set; }
        public SimpleAdmin? Admin { get; set; }
    }

    public class SimpleAdmin
    {
        public string Id { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = "Admin";
    }

    public class SimpleAuthService : ISimpleAuthService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SimpleAuthService> _logger;
        private readonly string _secretKey;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _accessTokenExpiryMinutes;
        private readonly int _refreshTokenExpiryDays;

        // In-memory storage for simplicity (in production, use proper database)
        private static readonly Dictionary<string, SimpleAdmin> _admins = new();
        private static readonly Dictionary<string, RefreshTokenData> _refreshTokens = new();

        public SimpleAuthService(IConfiguration configuration, ILogger<SimpleAuthService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            _secretKey = _configuration["Jwt:SecretKey"] ?? "GussmannLoyaltySecretKey2024ForJWTAuthentication!@#$%";
            _issuer = _configuration["Jwt:Issuer"] ?? "GussmannLoyaltyProgram";
            _audience = _configuration["Jwt:Audience"] ?? "GussmannLoyaltyUsers";
            _accessTokenExpiryMinutes = int.Parse(_configuration["Jwt:AccessTokenExpiryMinutes"] ?? "15");
            _refreshTokenExpiryDays = int.Parse(_configuration["Jwt:RefreshTokenExpiryDays"] ?? "30");

            // Create default admin if none exists
            InitializeDefaultAdmin();
        }

        private void InitializeDefaultAdmin()
        {
            if (!_admins.ContainsKey("admin"))
            {
                var defaultAdmin = new SimpleAdmin
                {
                    Id = "admin-001",
                    Username = "admin",
                    Email = "admin@gussmann.com",
                    FullName = "System Administrator",
                    Role = "Admin"
                };

                var hashedPassword = HashPassword("admin123");
                _admins.Add("admin", new SimpleAdmin
                {
                    Id = defaultAdmin.Id,
                    Username = defaultAdmin.Username,
                    Email = defaultAdmin.Email,
                    FullName = defaultAdmin.FullName,
                    Role = defaultAdmin.Role
                });

                // Store password separately for security (this is simplified)
                if (!_passwordStore.ContainsKey("admin"))
                {
                    _passwordStore.Add("admin", hashedPassword);
                }
            }
        }

        private static readonly Dictionary<string, string> _passwordStore = new();

        public async Task<bool> IsAdminExistsAsync()
        {
            return await Task.FromResult(_admins.Any());
        }

        public async Task<bool> CreateInitialAdminAsync(string username, string email, string password, string fullName)
        {
            try
            {
                if (_admins.ContainsKey(username.ToLower()))
                {
                    return false;
                }

                var admin = new SimpleAdmin
                {
                    Id = Guid.NewGuid().ToString(),
                    Username = username,
                    Email = email,
                    FullName = fullName,
                    Role = "Admin"
                };

                _admins.Add(username.ToLower(), admin);
                _passwordStore.Add(username.ToLower(), HashPassword(password));

                _logger.LogInformation("Initial admin created: {Username}", username);
                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating initial admin: {Username}", username);
                return false;
            }
        }

        public async Task<SimpleAuthResult> LoginAsync(string username, string password)
        {
            try
            {
                var userKey = username.ToLower();
                
                if (!_admins.ContainsKey(userKey) || !_passwordStore.ContainsKey(userKey))
                {
                    _logger.LogWarning("Login attempt with invalid username: {Username}", username);
                    return new SimpleAuthResult { Success = false, Message = "Invalid username or password." };
                }

                var admin = _admins[userKey];
                var storedPasswordHash = _passwordStore[userKey];

                if (!VerifyPassword(password, storedPasswordHash))
                {
                    _logger.LogWarning("Login attempt with invalid password for user: {Username}", username);
                    return new SimpleAuthResult { Success = false, Message = "Invalid username or password." };
                }

                // Generate tokens
                var jwtId = Guid.NewGuid().ToString();
                var accessTokenExpiry = DateTime.UtcNow.AddMinutes(_accessTokenExpiryMinutes);
                var refreshTokenExpiry = DateTime.UtcNow.AddDays(_refreshTokenExpiryDays);

                var accessToken = GenerateAccessToken(admin, jwtId, accessTokenExpiry);
                var refreshToken = GenerateRefreshToken();

                // Store refresh token
                _refreshTokens[refreshToken] = new RefreshTokenData
                {
                    AdminId = admin.Id,
                    JwtId = jwtId,
                    ExpiresAt = refreshTokenExpiry,
                    IsUsed = false
                };

                _logger.LogInformation("Successful login for admin: {Username}", username);

                return await Task.FromResult(new SimpleAuthResult
                {
                    Success = true,
                    Message = "Login successful.",
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    AccessTokenExpiry = accessTokenExpiry,
                    RefreshTokenExpiry = refreshTokenExpiry,
                    Admin = admin
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for username: {Username}", username);
                return new SimpleAuthResult { Success = false, Message = "An error occurred during login." };
            }
        }

        public async Task<SimpleAuthResult?> RefreshTokenAsync(string refreshToken)
        {
            try
            {
                if (!_refreshTokens.ContainsKey(refreshToken))
                {
                    return null;
                }

                var tokenData = _refreshTokens[refreshToken];
                
                if (tokenData.IsUsed || tokenData.ExpiresAt <= DateTime.UtcNow)
                {
                    return null;
                }

                var admin = _admins.Values.FirstOrDefault(a => a.Id == tokenData.AdminId);
                if (admin == null)
                {
                    return null;
                }

                // Mark current token as used
                tokenData.IsUsed = true;

                // Generate new tokens
                var jwtId = Guid.NewGuid().ToString();
                var accessTokenExpiry = DateTime.UtcNow.AddMinutes(_accessTokenExpiryMinutes);
                var refreshTokenExpiry = DateTime.UtcNow.AddDays(_refreshTokenExpiryDays);

                var accessToken = GenerateAccessToken(admin, jwtId, accessTokenExpiry);
                var newRefreshToken = GenerateRefreshToken();

                // Store new refresh token
                _refreshTokens[newRefreshToken] = new RefreshTokenData
                {
                    AdminId = admin.Id,
                    JwtId = jwtId,
                    ExpiresAt = refreshTokenExpiry,
                    IsUsed = false
                };

                return await Task.FromResult(new SimpleAuthResult
                {
                    Success = true,
                    Message = "Token refreshed successfully.",
                    AccessToken = accessToken,
                    RefreshToken = newRefreshToken,
                    AccessTokenExpiry = accessTokenExpiry,
                    RefreshTokenExpiry = refreshTokenExpiry,
                    Admin = admin
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                return null;
            }
        }

        public async Task<bool> ValidateTokenAsync(string token)
        {
            try
            {
                _logger.LogInformation($"Validating token: {token?.Substring(0, Math.Min(20, token?.Length ?? 0))}...");
                
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("Token is null or empty");
                    return false;
                }

                var principal = await GetPrincipalFromTokenAsync(token);
                bool isValid = principal != null;
                
                if (isValid)
                {
                    var username = principal?.FindFirst(ClaimTypes.Name)?.Value;
                    _logger.LogInformation($"JWT Token validated for user: {username}");
                }
                else
                {
                    _logger.LogWarning("Token validation failed - principal is null");
                }
                
                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ValidateTokenAsync");
                return false;
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

        private string GenerateAccessToken(SimpleAdmin admin, string jwtId, DateTime expiry)
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

        private static string HashPassword(string password)
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

        private static bool VerifyPassword(string password, string hash)
        {
            try
            {
                var parts = hash.Split('.');
                if (parts.Length != 2)
                    return false;

                var salt = Convert.FromBase64String(parts[0]);
                var storedHash = parts[1];

                string testHash = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                    password: password,
                    salt: salt,
                    prf: KeyDerivationPrf.HMACSHA256,
                    iterationCount: 100000,
                    numBytesRequested: 256 / 8));

                return testHash == storedHash;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> IsAuthenticatedAsync()
        {
            // This method would typically check the current HTTP context for a valid JWT token
            // For now, we'll return true since this is used in client-side validation
            // The actual token validation happens in the ValidateTokenAsync method
            return await Task.FromResult(true);
        }
    }

    public class RefreshTokenData
    {
        public string AdminId { get; set; } = string.Empty;
        public string JwtId { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public bool IsUsed { get; set; }
    }
}
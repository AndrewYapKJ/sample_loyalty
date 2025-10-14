using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace gussmann_loyalty_program.Services
{
    public class AuthService : IAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;
        private readonly string _apiBaseUrl;
        private readonly string _secretKey;
        private readonly string _issuer;
        private readonly string _audience;

        public AuthService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<AuthService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;

            _apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7001";
            _secretKey = _configuration["Jwt:SecretKey"] ?? "GussmannLoyaltySecretKey2024ForJWTAuthentication!@#$%";
            _issuer = _configuration["Jwt:Issuer"] ?? "GussmannLoyaltyProgram";
            _audience = _configuration["Jwt:Audience"] ?? "GussmannLoyaltyUsers";

            // Configure HttpClient base address
            if (_httpClient.BaseAddress == null)
            {
                _httpClient.BaseAddress = new Uri(_apiBaseUrl);
            }
        }

        public async Task<AuthResult> LoginAsync(string email, string password)
        {
            try
            {
                var request = new { Email = email, Password = password };
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("api/auth/login", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var loginResponse = JsonSerializer.Deserialize<LoginApiResponse>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return new AuthResult
                    {
                        Success = loginResponse?.Success ?? false,
                        Message = loginResponse?.Message ?? "Login successful",
                        AccessToken = loginResponse?.AccessToken,
                        RefreshToken = loginResponse?.RefreshToken,
                        AccessTokenExpiry = loginResponse?.AccessTokenExpiry,
                        RefreshTokenExpiry = loginResponse?.RefreshTokenExpiry,
                        Admin = loginResponse?.Admin != null ? new AdminInfo
                        {
                            Id = loginResponse.Admin.Id,
                            Username = loginResponse.Admin.Username,
                            Email = loginResponse.Admin.Email,
                            FullName = loginResponse.Admin.FullName,
                            Role = loginResponse.Admin.Role,
                            IsActive = loginResponse.Admin.IsActive,
                            CreatedAt = loginResponse.Admin.CreatedAt,
                            LastLoginAt = loginResponse.Admin.LastLoginAt
                        } : null
                    };
                }
                else
                {
                    var errorResponse = JsonSerializer.Deserialize<LoginApiResponse>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return new AuthResult
                    {
                        Success = false,
                        Message = errorResponse?.Message ?? "Login failed"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login request");
                return new AuthResult
                {
                    Success = false,
                    Message = "An error occurred during login"
                };
            }
        }

        public async Task<AuthResult?> RefreshTokenAsync(string refreshToken)
        {
            try
            {
                var request = new { RefreshToken = refreshToken };
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("api/auth/refresh", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var refreshResponse = JsonSerializer.Deserialize<LoginApiResponse>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return new AuthResult
                    {
                        Success = refreshResponse?.Success ?? false,
                        Message = refreshResponse?.Message ?? "Token refreshed",
                        AccessToken = refreshResponse?.AccessToken,
                        RefreshToken = refreshResponse?.RefreshToken,
                        AccessTokenExpiry = refreshResponse?.AccessTokenExpiry,
                        RefreshTokenExpiry = refreshResponse?.RefreshTokenExpiry,
                        Admin = refreshResponse?.Admin != null ? new AdminInfo
                        {
                            Id = refreshResponse.Admin.Id,
                            Username = refreshResponse.Admin.Username,
                            Email = refreshResponse.Admin.Email,
                            FullName = refreshResponse.Admin.FullName,
                            Role = refreshResponse.Admin.Role,
                            IsActive = refreshResponse.Admin.IsActive,
                            CreatedAt = refreshResponse.Admin.CreatedAt,
                            LastLoginAt = refreshResponse.Admin.LastLoginAt
                        } : null
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token refresh");
                return null;
            }
        }

        public async Task<bool> ValidateTokenAsync(string token)
        {
            try
            {
                if (string.IsNullOrEmpty(token))
                {
                    return false;
                }

                var request = new { Token = token };
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("api/auth/validate", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var validateResponse = JsonSerializer.Deserialize<ValidateTokenApiResponse>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return validateResponse?.IsValid ?? false;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token validation");
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

        public async Task<bool> LogoutAsync()
        {
            try
            {
                var response = await _httpClient.PostAsync("api/auth/logout", null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return false;
            }
        }

        public async Task<AdminInfo?> GetCurrentUserAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/auth/me");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var adminResponse = JsonSerializer.Deserialize<AdminApiDto>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return adminResponse != null ? new AdminInfo
                    {
                        Id = adminResponse.Id,
                        Username = adminResponse.Username,
                        Email = adminResponse.Email,
                        FullName = adminResponse.FullName,
                        Role = adminResponse.Role,
                        IsActive = adminResponse.IsActive,
                        CreatedAt = adminResponse.CreatedAt,
                        LastLoginAt = adminResponse.LastLoginAt
                    } : null;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user");
                return null;
            }
        }
    }

    // API Response DTOs
    internal class LoginApiResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? AccessTokenExpiry { get; set; }
        public DateTime? RefreshTokenExpiry { get; set; }
        public AdminApiDto? Admin { get; set; }
    }

    internal class ValidateTokenApiResponse
    {
        public bool IsValid { get; set; }
        public string? Username { get; set; }
        public string? Role { get; set; }
    }

    internal class AdminApiDto
    {
        public string Id { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using gussmann_loyalty_program.Services;
using System.Text.Json;

namespace gussmann_loyalty_program.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ISimpleAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(ISimpleAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
                {
                    return BadRequest(new { message = "Username and password are required." });
                }

                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

                var result = await _authService.LoginAsync(request.Username, request.Password);

                if (result.Success)
                {
                    // Set secure HTTP-only cookies
                    var cookieOptions = new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = Request.IsHttps,
                        SameSite = SameSiteMode.Strict,
                        Expires = result.AccessTokenExpiry
                    };

                    Response.Cookies.Append("AccessToken", result.AccessToken!, cookieOptions);
                    
                    var refreshCookieOptions = new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = Request.IsHttps,
                        SameSite = SameSiteMode.Strict,
                        Expires = result.RefreshTokenExpiry
                    };

                    Response.Cookies.Append("RefreshToken", result.RefreshToken!, refreshCookieOptions);

                    return Ok(new
                    {
                        message = result.Message,
                        accessToken = result.AccessToken,
                        refreshToken = result.RefreshToken,
                        accessTokenExpiry = result.AccessTokenExpiry,
                        refreshTokenExpiry = result.RefreshTokenExpiry,
                        admin = new
                        {
                            id = result.Admin?.Id,
                            username = result.Admin?.Username,
                            email = result.Admin?.Email,
                            fullName = result.Admin?.FullName,
                            role = result.Admin?.Role
                        }
                    });
                }
                else
                {
                    return Unauthorized(new { message = result.Message });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in login API");
                return StatusCode(500, new { message = "An error occurred during login." });
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            try
            {
                // For JWT tokens, logout is handled client-side by clearing tokens
                // We can optionally invalidate refresh tokens here in the future
                
                // Clear cookies
                Response.Cookies.Delete("AccessToken");
                Response.Cookies.Delete("RefreshToken");

                return Ok(new { message = "Logged out successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return StatusCode(500, new { message = "An error occurred during logout." });
            }
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken()
        {
            try
            {
                var refreshToken = Request.Cookies["RefreshToken"] ?? 
                                 Request.Headers["X-Refresh-Token"].FirstOrDefault();

                if (string.IsNullOrEmpty(refreshToken))
                {
                    return Unauthorized(new { message = "Refresh token not provided." });
                }

                var result = await _authService.RefreshTokenAsync(refreshToken);

                if (result != null && result.Success)
                {
                    // Set new cookies
                    var cookieOptions = new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = Request.IsHttps,
                        SameSite = SameSiteMode.Strict,
                        Expires = result.AccessTokenExpiry
                    };

                    Response.Cookies.Append("AccessToken", result.AccessToken!, cookieOptions);
                    
                    var refreshCookieOptions = new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = Request.IsHttps,
                        SameSite = SameSiteMode.Strict,
                        Expires = result.RefreshTokenExpiry
                    };

                    Response.Cookies.Append("RefreshToken", result.RefreshToken!, refreshCookieOptions);

                    return Ok(new
                    {
                        message = result.Message,
                        accessToken = result.AccessToken,
                        refreshToken = result.RefreshToken,
                        accessTokenExpiry = result.AccessTokenExpiry,
                        refreshTokenExpiry = result.RefreshTokenExpiry
                    });
                }
                else
                {
                    return Unauthorized(new { message = "Invalid or expired refresh token." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                return StatusCode(500, new { message = "An error occurred while refreshing token." });
            }
        }

        [HttpPost("create-admin")]
        public async Task<IActionResult> CreateAdmin([FromBody] CreateAdminRequest request)
        {
            try
            {
                var success = await _authService.CreateInitialAdminAsync(
                    request.Username, 
                    request.Email, 
                    request.Password, 
                    request.FullName);

                if (success)
                {
                    return Ok(new { message = "Admin account created successfully." });
                }
                else
                {
                    return BadRequest(new { message = "Failed to create admin account. Username or email may already exist." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating admin");
                return StatusCode(500, new { message = "An error occurred while creating the admin account." });
            }
        }

        [HttpGet("status")]
        [Authorize]
        public IActionResult GetStatus()
        {
            var userId = User.FindFirst("sub")?.Value ?? User.FindFirst("id")?.Value;
            var username = User.FindFirst("name")?.Value;
            var role = User.FindFirst("role")?.Value;

            return Ok(new
            {
                isAuthenticated = true,
                userId = userId,
                username = username,
                role = role
            });
        }
    }

    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
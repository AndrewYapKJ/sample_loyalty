using App.Api.DTOs;
using App.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthenticationService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthenticationService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _authService.LoginAsync(request);
                
                if (!result.Success)
                {
                    return Unauthorized(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login attempt");
                return StatusCode(500, new LoginResponse 
                { 
                    Success = false, 
                    Message = "An internal server error occurred." 
                });
            }
        }

        [HttpPost("refresh")]
        public async Task<ActionResult<LoginResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _authService.RefreshTokenAsync(request);
                
                if (result == null)
                {
                    return Unauthorized(new LoginResponse 
                    { 
                        Success = false, 
                        Message = "Invalid or expired refresh token." 
                    });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token refresh");
                return StatusCode(500, new LoginResponse 
                { 
                    Success = false, 
                    Message = "An internal server error occurred." 
                });
            }
        }

        [HttpPost("validate")]
        public async Task<ActionResult<ValidateTokenResponse>> ValidateToken([FromBody] ValidateTokenRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _authService.ValidateTokenAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token validation");
                return StatusCode(500, new ValidateTokenResponse 
                { 
                    IsValid = false 
                });
            }
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<ActionResult> Logout()
        {
            try
            {
                // In a real implementation, you might want to blacklist the token
                // For now, we'll just return success as the client will remove the token
                return Ok(new { success = true, message = "Logged out successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return StatusCode(500, new { success = false, message = "An error occurred during logout." });
            }
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<AdminDto>> GetCurrentUser()
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                // In a real implementation, you would fetch the user from the database
                // For now, return the claims data
                var userDto = new AdminDto
                {
                    Id = userId,
                    Username = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "",
                    Email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "",
                    FullName = User.FindFirst("full_name")?.Value ?? "",
                    Role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "",
                    IsActive = true
                };

                return Ok(userDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user");
                return StatusCode(500, new { message = "An error occurred while retrieving user information." });
            }
        }
    }
}
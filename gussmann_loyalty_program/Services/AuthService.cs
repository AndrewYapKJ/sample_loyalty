using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Cryptography;
using gussmann_loyalty_program.Data;
using gussmann_loyalty_program.Models;

namespace gussmann_loyalty_program.Services
{
    public interface IAuthService
    {
        Task<AuthResult> LoginAsync(string username, string password, string? ipAddress = null, string? userAgent = null);
        Task<bool> LogoutAsync(string refreshToken);
        Task<AuthResult?> RefreshTokenAsync(string refreshToken);
        Task<bool> CreateAdminAsync(CreateAdminRequest request);
        Task<bool> ChangePasswordAsync(string adminId, string currentPassword, string newPassword);
        Task<bool> ResetPasswordAsync(string email);
        Task<Admin?> GetAdminByIdAsync(string adminId);
        Task<List<Admin>> GetAllAdminsAsync();
        Task<bool> ToggleAdminStatusAsync(string adminId);
        Task LogAdminActionAsync(string adminId, string action, string? details = null, string? ipAddress = null, string? userAgent = null);
    }

    public class AuthResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? AccessTokenExpiry { get; set; }
        public DateTime? RefreshTokenExpiry { get; set; }
        public Admin? Admin { get; set; }
    }

    public class CreateAdminRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = "Admin";
    }

    public class AuthService : IAuthService
    {
        private readonly NewLoyaltyDbContext _context;
        private readonly IJwtService _jwtService;
        private readonly ILogger<AuthService> _logger;
        private const int MaxLoginAttempts = 5;
        private const int LockoutMinutes = 30;

        public AuthService(NewLoyaltyDbContext context, IJwtService jwtService, ILogger<AuthService> logger)
        {
            _context = context;
            _jwtService = jwtService;
            _logger = logger;
        }

        public async Task<AuthResult> LoginAsync(string username, string password, string? ipAddress = null, string? userAgent = null)
        {
            try
            {
                var admin = await _context.Admins
                    .FirstOrDefaultAsync(a => a.Username.ToLower() == username.ToLower() && a.IsActive);

                if (admin == null)
                {
                    _logger.LogWarning("Login attempt with invalid username: {Username} from IP: {IpAddress}", username, ipAddress);
                    return new AuthResult { Success = false, Message = "Invalid username or password." };
                }

                // Check if account is locked
                if (admin.LockedUntil.HasValue && admin.LockedUntil > DateTime.UtcNow)
                {
                    var lockTimeRemaining = admin.LockedUntil.Value.Subtract(DateTime.UtcNow);
                    _logger.LogWarning("Login attempt on locked account: {Username} from IP: {IpAddress}", username, ipAddress);
                    return new AuthResult 
                    { 
                        Success = false, 
                        Message = $"Account is locked. Try again in {lockTimeRemaining.Minutes} minutes." 
                    };
                }

                // Verify password
                if (!VerifyPassword(password, admin.PasswordHash))
                {
                    // Increment login attempts
                    admin.LoginAttempts++;
                    
                    if (admin.LoginAttempts >= MaxLoginAttempts)
                    {
                        admin.LockedUntil = DateTime.UtcNow.AddMinutes(LockoutMinutes);
                        _logger.LogWarning("Account locked due to too many failed attempts: {Username} from IP: {IpAddress}", username, ipAddress);
                    }

                    await _context.SaveChangesAsync();
                    await LogAdminActionAsync(admin.Id, "LOGIN_FAILED", $"Failed login attempt from {ipAddress}", ipAddress, userAgent);

                    return new AuthResult { Success = false, Message = "Invalid username or password." };
                }

                // Reset login attempts on successful login
                admin.LoginAttempts = 0;
                admin.LockedUntil = null;
                admin.LastLoginAt = DateTime.UtcNow;

                // Generate JWT tokens
                var tokenResult = await _jwtService.GenerateTokenAsync(admin);

                await _context.SaveChangesAsync();
                await LogAdminActionAsync(admin.Id, "LOGIN_SUCCESS", $"Successful login from {ipAddress}", ipAddress, userAgent);

                _logger.LogInformation("Successful login for admin: {Username} from IP: {IpAddress}", username, ipAddress);

                return new AuthResult
                {
                    Success = true,
                    Message = "Login successful.",
                    AccessToken = tokenResult.AccessToken,
                    RefreshToken = tokenResult.RefreshToken,
                    AccessTokenExpiry = tokenResult.AccessTokenExpiry,
                    RefreshTokenExpiry = tokenResult.RefreshTokenExpiry,
                    Admin = admin
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for username: {Username}", username);
                return new AuthResult { Success = false, Message = "An error occurred during login." };
            }
        }

        public async Task<bool> LogoutAsync(string refreshToken)
        {
            try
            {
                var tokenRecord = await _context.RefreshTokens
                    .Include(rt => rt.Admin)
                    .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

                if (tokenRecord != null)
                {
                    await _jwtService.RevokeTokenAsync(refreshToken);
                    await LogAdminActionAsync(tokenRecord.AdminId, "LOGOUT", "User logged out");
                    _logger.LogInformation("Admin logged out: {AdminId}", tokenRecord.AdminId);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return false;
            }
        }

        public async Task<AuthResult?> RefreshTokenAsync(string refreshToken)
        {
            try
            {
                var tokenResult = await _jwtService.RefreshTokenAsync(refreshToken);
                
                if (tokenResult == null)
                {
                    return new AuthResult { Success = false, Message = "Invalid or expired refresh token." };
                }

                var tokenRecord = await _context.RefreshTokens
                    .Include(rt => rt.Admin)
                    .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

                if (tokenRecord?.Admin != null)
                {
                    await LogAdminActionAsync(tokenRecord.AdminId, "TOKEN_REFRESH", "Access token refreshed");
                }

                return new AuthResult
                {
                    Success = true,
                    Message = "Token refreshed successfully.",
                    AccessToken = tokenResult.AccessToken,
                    RefreshToken = tokenResult.RefreshToken,
                    AccessTokenExpiry = tokenResult.AccessTokenExpiry,
                    RefreshTokenExpiry = tokenResult.RefreshTokenExpiry,
                    Admin = tokenRecord?.Admin
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                return new AuthResult { Success = false, Message = "Error refreshing token." };
            }
        }

        public async Task<bool> CreateAdminAsync(CreateAdminRequest request)
        {
            try
            {
                // Check if username or email already exists
                var existingAdmin = await _context.Admins
                    .AnyAsync(a => a.Username.ToLower() == request.Username.ToLower() || 
                                  a.Email.ToLower() == request.Email.ToLower());

                if (existingAdmin)
                {
                    return false;
                }

                var admin = new Admin
                {
                    Username = request.Username,
                    Email = request.Email,
                    PasswordHash = HashPassword(request.Password),
                    FullName = request.FullName,
                    Role = request.Role
                };

                _context.Admins.Add(admin);
                await _context.SaveChangesAsync();

                await LogAdminActionAsync(admin.Id, "ADMIN_CREATED", $"New admin account created: {request.Username}");
                _logger.LogInformation("New admin created: {Username}", request.Username);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating admin: {Username}", request.Username);
                return false;
            }
        }

        public async Task<bool> ChangePasswordAsync(string adminId, string currentPassword, string newPassword)
        {
            try
            {
                var admin = await _context.Admins.FindAsync(adminId);
                
                if (admin == null || !VerifyPassword(currentPassword, admin.PasswordHash))
                {
                    return false;
                }

                admin.PasswordHash = HashPassword(newPassword);
                await _context.SaveChangesAsync();

                // Revoke all existing tokens to force re-login
                await _jwtService.RevokeAllUserTokensAsync(adminId);

                await LogAdminActionAsync(adminId, "PASSWORD_CHANGED", "Password changed successfully");
                _logger.LogInformation("Password changed for admin: {AdminId}", adminId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for admin: {AdminId}", adminId);
                return false;
            }
        }

        public async Task<bool> ResetPasswordAsync(string email)
        {
            try
            {
                var admin = await _context.Admins
                    .FirstOrDefaultAsync(a => a.Email.ToLower() == email.ToLower());

                if (admin == null)
                {
                    // Don't reveal that email doesn't exist
                    return true;
                }

                // Generate temporary password
                var tempPassword = GenerateTemporaryPassword();
                admin.PasswordHash = HashPassword(tempPassword);
                admin.LoginAttempts = 0;
                admin.LockedUntil = null;

                await _context.SaveChangesAsync();

                // In a real application, you would send this via email
                _logger.LogInformation("Password reset for admin: {Email}. Temporary password: {TempPassword}", email, tempPassword);
                await LogAdminActionAsync(admin.Id, "PASSWORD_RESET", "Password reset requested");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for email: {Email}", email);
                return false;
            }
        }

        public async Task<Admin?> GetAdminByIdAsync(string adminId)
        {
            return await _context.Admins.FindAsync(adminId);
        }

        public async Task<List<Admin>> GetAllAdminsAsync()
        {
            return await _context.Admins
                .OrderBy(a => a.Username)
                .ToListAsync();
        }

        public async Task<bool> ToggleAdminStatusAsync(string adminId)
        {
            try
            {
                var admin = await _context.Admins.FindAsync(adminId);
                
                if (admin == null)
                    return false;

                admin.IsActive = !admin.IsActive;
                
                if (!admin.IsActive)
                {
                    // Revoke all tokens when deactivating
                    await _jwtService.RevokeAllUserTokensAsync(adminId);
                }

                await _context.SaveChangesAsync();
                await LogAdminActionAsync(adminId, "STATUS_CHANGED", $"Admin status changed to: {(admin.IsActive ? "Active" : "Inactive")}");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling admin status: {AdminId}", adminId);
                return false;
            }
        }

        public async Task LogAdminActionAsync(string adminId, string action, string? details = null, string? ipAddress = null, string? userAgent = null)
        {
            try
            {
                var auditLog = new AdminAuditLog
                {
                    AdminId = adminId,
                    Action = action,
                    Details = details,
                    IpAddress = ipAddress,
                    UserAgent = userAgent
                };

                _context.AdminAuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging admin action: {Action} for admin: {AdminId}", action, adminId);
            }
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

        private static string GenerateTemporaryPassword()
        {
            const string chars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 12)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
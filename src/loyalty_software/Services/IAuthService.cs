using System.Security.Claims;
using System.Threading.Tasks;

namespace loyalty_sfotware.Services
{
    public interface IAuthService
    {
        Task<AuthResult> LoginAsync(string email, string password);
        Task<AuthResult?> RefreshTokenAsync(string refreshToken);
        Task<bool> ValidateTokenAsync(string token);
        Task<ClaimsPrincipal?> GetPrincipalFromTokenAsync(string token);
        Task<bool> LogoutAsync();
        Task<AdminInfo?> GetCurrentUserAsync();
    }
}

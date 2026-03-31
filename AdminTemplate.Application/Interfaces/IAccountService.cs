using System.Threading.Tasks;
using AdminTemplate.Application.Services;

namespace AdminTemplate.Application.Interfaces
{
    public interface IAccountService
    {
        Task<LoginResult> LoginAsync(string email, string password, bool rememberMe);

        Task<RegisterResult> RegisterAsync(string fullName, string email, string password);

        Task ForgotPasswordAsync(string email, string resetUrlBase);

        Task<ResetPasswordResult> ResetPasswordAsync(string email, string token, string newPassword);

        Task LogoutAsync();
    }
}

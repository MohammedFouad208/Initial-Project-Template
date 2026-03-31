using System.Threading.Tasks;

namespace AdminTemplate.Application.Providers
{
    public interface IEmailSender
    {
        Task SendAsync(string toEmail, string subject, string htmlBody);
    }
}

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using AdminTemplate.Application.Providers;

namespace AdminTemplate.Infrastructure.Providers
{
    public class NoOpEmailSender : IEmailSender
    {
        public Task SendAsync(string toEmail, string subject, string htmlBody)
        {
            var message = $"[NoOpEmailSender] To: {toEmail} | Subject: {subject}\n{htmlBody}";
            Debug.WriteLine(message);
            Console.WriteLine(message);
            return Task.CompletedTask;
        }
    }
}

using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;

namespace HealthCheckAPI.Notifications
{
    public class Email

    {
        private readonly IConfiguration _configuration;

        public Email(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void SendEmail(string to, string subject, string body)
        {
            Console.WriteLine("SendEmail method was called.");
            try
            {

                var smtpHost = _configuration["EmailSettings:SmtpHost"];
                var smtpPort = _configuration["EmailSettings:SmtpPort"];
                var username = _configuration["EmailSettings:Username"];
                var password = _configuration["EmailSettings:Password"];

                Console.WriteLine($"SMTP Host: {smtpHost}, Port: {smtpPort}, Username: {username}");

                if (smtpHost is null)
                {
                    return;
                }
                int port = int.Parse(smtpPort);

                using var client = new SmtpClient(smtpHost, port)
                {
                    Credentials = new NetworkCredential(username, password),
                    EnableSsl = true
                };

                var mailMessage = new MailMessage(username, to, subject, body)
                {
                    IsBodyHtml = false
                };

                client.Send(mailMessage);

                Console.WriteLine("Email sent successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to send email:" + ex.Message);
            }

        }
    }
}

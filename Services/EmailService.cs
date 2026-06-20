using System.Net;
using System.Net.Mail;
using System.Text;

namespace AutoGarageManager.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var smtpServer = _configuration["EmailSettings:SmtpServer"] ?? "smtp.gmail.com";
            var smtpPort = int.TryParse(_configuration["EmailSettings:SmtpPort"], out var port) ? port : 587;
            var senderName = _configuration["EmailSettings:SenderName"] ?? "Auto Garage";
            var senderEmail = _configuration["EmailSettings:SenderEmail"];
            var senderPassword = _configuration["EmailSettings:SenderPassword"];

            if (string.IsNullOrWhiteSpace(senderEmail) || string.IsNullOrWhiteSpace(senderPassword) || senderPassword.Contains("APP_PASSWORD"))
                throw new InvalidOperationException("Chưa cấu hình EmailSettings SenderEmail/SenderPassword trong appsettings.json");

            using var message = new MailMessage
            {
                From = new MailAddress(senderEmail, senderName, Encoding.UTF8),
                Subject = subject,
                Body = body,
                IsBodyHtml = true,
                BodyEncoding = Encoding.UTF8,
                SubjectEncoding = Encoding.UTF8
            };
            message.To.Add(toEmail);

            using var smtp = new SmtpClient(smtpServer, smtpPort)
            {
                Credentials = new NetworkCredential(senderEmail, senderPassword.Replace(" ", "")),
                EnableSsl = true
            };

            await smtp.SendMailAsync(message);
        }
    }
}

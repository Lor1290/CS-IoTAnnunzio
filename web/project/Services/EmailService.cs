using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;

namespace project.Services;

public class EmailService {
    private readonly IConfiguration _config;
    public EmailService(IConfiguration config) => _config = config;

    public async Task SendAsync(string to, string subject, string body) {
        var s = _config.GetSection("Email");

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(s["FromName"], s["From"]));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body    = new TextPart("html") { Text = body };

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(s["Host"], int.Parse(s["Port"]!), SecureSocketOptions.StartTls);
        await smtp.AuthenticateAsync(s["Username"], s["Password"]);
        await smtp.SendAsync(message);
        await smtp.DisconnectAsync(true);
    }
}

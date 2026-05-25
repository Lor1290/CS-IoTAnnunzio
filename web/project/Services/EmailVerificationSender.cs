using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace project.Services;

public interface IEmailVerificationSender {
    Task SendVerificationCodeAsync(string toEmail, string toName, string verificationCode);
}

public sealed class EmailVerificationSender : IEmailVerificationSender {
    private readonly EmailSettings _settings;

    public EmailVerificationSender(IOptions<EmailSettings> settings) {
        _settings = settings.Value;
    }

    public async Task SendVerificationCodeAsync(string toEmail, string toName, string verificationCode) {
        ValidateSettings();

        try {
            using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort) {
                UseDefaultCredentials = false,
                EnableSsl = _settings.EnableSsl,
                Credentials = new NetworkCredential(_settings.Username, _settings.Password)
            };

            using var message = new MailMessage {
                From = new MailAddress(_settings.FromAddress, _settings.FromName),
                Subject = "Codice di verifica accesso",
                Body = $@"
Ciao {toName},
bentornato nella tua Dashboard Sensori 👋
Ecco il tuo codice di verifica:

    {verificationCode}

Inseriscilo nella pagina di accesso per entrare.
Il codice è valido per questa sessione soltanto.
Se non hai richiesto l'accesso, ignora pure questa email.

— Il team IoT Annunzio",
                IsBodyHtml = false
            };

            message.To.Add(toEmail);

            await client.SendMailAsync(message);
        } catch (SmtpException ex) {
            if (ex.Message.Contains("5.7.0", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("Authentication Required", StringComparison.OrdinalIgnoreCase)) {
                throw new InvalidOperationException("Gmail ha rifiutato l'autenticazione. Usa una app password Google, non la password normale dell'account, e verifica che l'account abbia la verifica in due passaggi attiva.", ex);
            }

            throw new InvalidOperationException($"Invio email fallito su {_settings.SmtpHost}:{_settings.SmtpPort}. {ex.Message}", ex);
        }
    }

    private void ValidateSettings() {
        if (string.IsNullOrWhiteSpace(_settings.SmtpHost) ||
            string.IsNullOrWhiteSpace(_settings.Username) ||
            string.IsNullOrWhiteSpace(_settings.Password) ||
            string.IsNullOrWhiteSpace(_settings.FromAddress)) {
            throw new InvalidOperationException("SMTP non configurato. Compila la sezione Email in appsettings.json.");
        }

        if (IsPlaceholder(_settings.SmtpHost) ||
            IsPlaceholder(_settings.Username) ||
            IsPlaceholder(_settings.Password) ||
            IsPlaceholder(_settings.FromAddress)) {
            throw new InvalidOperationException("SMTP ancora configurato con valori segnaposto. Sostituisci smtp.example.com e gli altri campi nella sezione Email con valori reali.");
        }
    }

    private static bool IsPlaceholder(string value) {
        return value.Contains("example.com", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("your-", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("INSERISCI", StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class EmailSettings {
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "CS-IoTAnnunzio";
}
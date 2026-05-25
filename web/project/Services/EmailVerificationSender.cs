using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace project.Services;

public interface IEmailVerificationSender {
    Task SendVerificationCodeAsync(string toEmail, string toName, string verificationCode);
}

public sealed class EmailVerificationSender : IEmailVerificationSender {
    private readonly EmailSettings _settings;
    private readonly HttpClient _http;

    public EmailVerificationSender(IOptions<EmailSettings> settings) {
        _settings = settings.Value;
        _http = new HttpClient();
        _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {_settings.Password}");
    }

    public async Task SendVerificationCodeAsync(string toEmail, string toName, string verificationCode) {
        var payload = new {
            from = $"{_settings.FromName} <{_settings.FromAddress}>",
            to = new[] { toEmail },
            subject = "Codice di verifica accesso",
            text = $@"Ciao {toName},

bentornato nella tua Dashboard Sensori 👋
Ecco il tuo codice di verifica:

    {verificationCode}

Inseriscilo nella pagina di accesso per entrare.
Il codice è valido per questa sessione soltanto.
Se non hai richiesto l'accesso, ignora pure questa email.

— Il team IoT Annunzio"
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _http.PostAsync("https://api.resend.com/emails", content);

        if (!response.IsSuccessStatusCode) {
            var error = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Resend API error {response.StatusCode}: {error}");
        }
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
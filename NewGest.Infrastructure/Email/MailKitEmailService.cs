using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using MimeKit.Text;
using NewGest.Application.Services;

namespace NewGest.Infrastructure.Email;

/// <summary>
/// Reemplaza CsFoxySmtp.dll del sistema VFP.
/// Usa MailKit — compatible con Gmail, Office 365, Brevo, SendGrid SMTP relay.
/// </summary>
public class MailKitEmailService : IEmailService
{
    private readonly SmtpConfig _config;

    public MailKitEmailService(IConfiguration configuration)
    {
        _config = new SmtpConfig(
            HostSmtp:     configuration["Email:Host"]     ?? throw new InvalidOperationException("Email:Host no configurado."),
            Puerto:       int.Parse(configuration["Email:Puerto"] ?? "587"),
            UsarSsl:      bool.Parse(configuration["Email:Ssl"]   ?? "true"),
            UsuarioSmtp:  configuration["Email:Usuario"]  ?? throw new InvalidOperationException("Email:Usuario no configurado."),
            PasswordSmtp: configuration["Email:Password"] ?? throw new InvalidOperationException("Email:Password no configurado."),
            NombreFrom:   configuration["Email:NombreFrom"] ?? "NewGest ERP"
        );
    }

    public async Task EnviarAsync(EmailMessage mensaje, CancellationToken ct)
    {
        var email = new MimeMessage();
        email.From.Add(new MailboxAddress(_config.NombreFrom, _config.UsuarioSmtp));

        foreach (var dest in mensaje.Destinatarios)
            email.To.Add(MailboxAddress.Parse(dest));

        if (!string.IsNullOrEmpty(mensaje.CC))
            email.Cc.Add(MailboxAddress.Parse(mensaje.CC));

        email.Subject = mensaje.Asunto;

        var builder = new BodyBuilder { HtmlBody = mensaje.CuerpoHtml };

        foreach (var adj in mensaje.Adjuntos ?? [])
            builder.Attachments.Add(adj.Nombre, adj.Contenido,
                MimeKit.ContentType.Parse(adj.MimeType));

        email.Body = builder.ToMessageBody();

        using var smtp = new SmtpClient();
        // Fix BUG-3: StartTlsWhenAvailable acepta conexiones sin cifrado si el servidor
        // no anuncia STARTTLS — credenciales SMTP quedarían en claro.
        // StartTls fuerza el upgrade y falla si el servidor no lo soporta.
        var secureOption = _config.UsarSsl
            ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.StartTls;

        await smtp.ConnectAsync(_config.HostSmtp, _config.Puerto, secureOption, ct);
        await smtp.AuthenticateAsync(_config.UsuarioSmtp, _config.PasswordSmtp, ct);
        await smtp.SendAsync(email, ct);
        await smtp.DisconnectAsync(quit: true, ct);
    }
}

namespace NewGest.Application.Services;

public interface IEmailService
{
    Task EnviarAsync(EmailMessage mensaje, CancellationToken ct);
}

public record EmailMessage(
    List<string> Destinatarios,
    string Asunto,
    string CuerpoHtml,
    string? CC = null,
    List<EmailAdjunto>? Adjuntos = null
);

public record EmailAdjunto(string Nombre, byte[] Contenido, string MimeType);

public record SmtpConfig(
    string HostSmtp,
    int Puerto,
    bool UsarSsl,
    string UsuarioSmtp,
    string PasswordSmtp,
    string NombreFrom
);

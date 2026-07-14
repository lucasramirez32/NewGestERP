namespace NewGest.Application.DTOs.Notificaciones;

public record EnviarComprobanteDto(
    int IdComprobante,
    bool EnviarEmail,
    bool EnviarWhatsApp,
    string? EmailDestino,      // null = usar email del cliente
    string? TelefonoDestino    // null = usar teléfono del cliente
);

public record ResultadoEnvioDto(
    bool EmailEnviado,
    bool WhatsAppEnviado,
    string? ErrorEmail,
    string? ErrorWhatsApp
);

namespace NewGest.Application.Services;

public interface IWhatsAppService
{
    Task EnviarComprobanteAsync(string telefono, string urlPdf, string numeroFactura, CancellationToken ct);
    Task EnviarMensajeTextoAsync(string telefono, string mensaje, CancellationToken ct);
}

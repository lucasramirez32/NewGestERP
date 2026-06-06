using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NewGest.Application.Services;

namespace NewGest.Infrastructure.Email;

/// <summary>
/// Integración con Meta Cloud API (WhatsApp Business) — reemplaza whatsapp.prg (API no oficial).
/// Requiere cuenta Business verificada en Meta y templates aprobados.
/// Documentación: https://developers.facebook.com/docs/whatsapp/cloud-api
/// </summary>
public class WhatsAppService : IWhatsAppService
{
    private readonly HttpClient _http;
    private readonly string _phoneNumberId;
    private readonly string _templateComprobante;
    private readonly ILogger<WhatsAppService> _logger;

    public WhatsAppService(IHttpClientFactory httpFactory, IConfiguration config, ILogger<WhatsAppService> logger)
    {
        _http = httpFactory.CreateClient("WhatsApp");
        var token = config["WhatsApp:AccessToken"]
            ?? throw new InvalidOperationException("WhatsApp:AccessToken no configurado.");
        _phoneNumberId      = config["WhatsApp:PhoneNumberId"]   ?? throw new InvalidOperationException("WhatsApp:PhoneNumberId no configurado.");
        _templateComprobante = config["WhatsApp:TemplateComprobante"] ?? "comprobante_disponible";

        _http.BaseAddress = new Uri("https://graph.facebook.com/v19.0/");
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        _logger = logger;
    }

    /// <summary>
    /// Envía el template aprobado "comprobante_disponible" con el número de factura y URL del PDF.
    /// El template debe tener 2 parámetros: {{1}} = número factura, {{2}} = URL PDF.
    /// </summary>
    public async Task EnviarComprobanteAsync(
        string telefono, string urlPdf, string numeroFactura, CancellationToken ct)
    {
        var payload = new
        {
            messaging_product = "whatsapp",
            to = NormalizarTelefono(telefono),
            type = "template",
            template = new
            {
                name = _templateComprobante,
                language = new { code = "es_AR" },
                components = new[]
                {
                    new
                    {
                        type = "body",
                        parameters = new[]
                        {
                            new { type = "text", text = numeroFactura },
                            new { type = "text", text = urlPdf }
                        }
                    }
                }
            }
        };

        await PostMensajeAsync(payload, ct);
        _logger.LogInformation("WhatsApp enviado a {Telefono}: comprobante {Numero}", telefono, numeroFactura);
    }

    public async Task EnviarMensajeTextoAsync(string telefono, string mensaje, CancellationToken ct)
    {
        var payload = new
        {
            messaging_product = "whatsapp",
            to = NormalizarTelefono(telefono),
            type = "text",
            text = new { body = mensaje }
        };

        await PostMensajeAsync(payload, ct);
    }

    private async Task PostMensajeAsync(object payload, CancellationToken ct)
    {
        var json     = JsonSerializer.Serialize(payload);
        var content  = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _http.PostAsync($"{_phoneNumberId}/messages", content, ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Error Meta Cloud API {Status}: {Body}", response.StatusCode, body);
            throw new InvalidOperationException($"Error WhatsApp API ({response.StatusCode}): {body}");
        }
    }

    // Convierte 011-1234-5678 o 1134567890 → 5491134567890 (formato E.164 Argentina)
    private static string NormalizarTelefono(string tel)
    {
        var digitos = new string(tel.Where(char.IsDigit).ToArray());
        if (digitos.StartsWith("549")) return digitos;
        if (digitos.StartsWith("54"))  return $"549{digitos[2..]}";
        if (digitos.StartsWith("0"))   return $"549{digitos[1..]}";
        return $"549{digitos}";
    }
}

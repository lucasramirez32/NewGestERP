using MediatR;
using NewGest.Application.DTOs.Notificaciones;
using NewGest.Application.DTOs.Reportes;
using NewGest.Application.Features.Clientes.Queries.GetClienteById;
using NewGest.Application.Features.Comprobantes.Queries.GetComprobanteById;
using NewGest.Application.Interfaces;
using NewGest.Application.Services;

namespace NewGest.Api.Endpoints;

public static class NotificacionesEndpoints
{
    public static IEndpointRouteBuilder MapNotificacionesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/notificaciones")
            .WithTags("Notificaciones")
            .RequireAuthorization();

        // POST /api/notificaciones/enviar-comprobante
        group.MapPost("/enviar-comprobante", async (
            EnviarComprobanteDto dto,
            ICurrentUserService cu, IMediator m,
            IEmailService emailService,
            IWhatsAppService whatsAppService,
            IReportService reportService,
            IQrFiscalService qrService,
            IEmpresaRepository empresasRepo,
            CancellationToken ct) =>
        {
            var comp    = await m.Send(new GetComprobanteByIdQuery(dto.IdComprobante, cu.IdEmpresa), ct);
            if (comp is null) return Results.NotFound("Comprobante no encontrado.");

            var empresa = await empresasRepo.ObtenerPorIdAsync(cu.IdEmpresa, ct);
            var cliente = await m.Send(new GetClienteByIdQuery(comp.IdCliente, cu.IdEmpresa), ct);

            string? errorEmail    = null;
            string? errorWhatsApp = null;
            var emailEnviado      = false;
            var whatsAppEnviado   = false;

            // Generar PDF del comprobante
            byte[]? pdfBytes = null;
            if (dto.EnviarEmail)
            {
                try
                {
                    // Construir DTO de reporte mínimo (sin QR para el adjunto)
                    var reportDto = new ComprobanteReportDto(
                        empresa?.Nombre ?? "", empresa?.RazonSocial ?? empresa?.Nombre ?? "",
                        empresa?.Cuit ?? "", "Responsable Inscripto", "",
                        comp.Tipo, comp.PuntoVenta, comp.Numero, comp.Fecha,
                        comp.RazonSocialCliente, comp.CuitCliente, comp.CondicionIvaReceptor, null,
                        comp.Items.Select(i => new ItemReportDto(
                            i.Descripcion, i.Cantidad, i.PrecioUnitario, i.Alicuota, i.Subtotal)).ToList(),
                        comp.Items.GroupBy(i => i.Alicuota)
                            .Select(g => new AlicuotaReportDto(
                                g.Key, g.Sum(i => i.SubtotalNeto), g.Sum(i => i.Iva))).ToList(),
                        comp.TotalNeto, comp.TotalIva, comp.Total,
                        comp.CodigoCae, comp.FechaVencimientoCae,
                        QrUrl: null, QrPngBytes: null);

                    pdfBytes = await reportService.GenerarComprobanteAsync(reportDto, ct);
                }
                catch (Exception ex) { errorEmail = $"Error generando PDF: {ex.Message}"; }
            }

            // Enviar email
            if (dto.EnviarEmail && pdfBytes is not null)
            {
                var destEmail = dto.EmailDestino ?? cliente?.Email;
                if (string.IsNullOrEmpty(destEmail))
                {
                    errorEmail = "El cliente no tiene email registrado.";
                }
                else
                {
                    try
                    {
                        var tipoLabel = comp.Tipo.Replace("Factura", "Factura ").Replace("Nota", "Nota ");
                        var asunto    = $"{tipoLabel} Nº {comp.PuntoVenta:D4}-{comp.Numero:D8} — {empresa?.Nombre}";
                        var cuerpo    = $"""
                            <p>Estimado/a <strong>{comp.RazonSocialCliente}</strong>,</p>
                            <p>Adjuntamos el comprobante {tipoLabel} N° {comp.PuntoVenta:D4}-{comp.Numero:D8}
                            por un total de <strong>${comp.Total:N2}</strong>.</p>
                            {(comp.CodigoCae is not null ? $"<p>CAE: {comp.CodigoCae} — Vence: {comp.FechaVencimientoCae:dd/MM/yyyy}</p>" : "")}
                            <p>Ante cualquier consulta, no dude en contactarnos.</p>
                            <p>Saludos,<br/><strong>{empresa?.Nombre}</strong></p>
                            """;

                        await emailService.EnviarAsync(new EmailMessage(
                            Destinatarios: [destEmail],
                            Asunto: asunto,
                            CuerpoHtml: cuerpo,
                            Adjuntos: [new EmailAdjunto(
                                $"comprobante_{comp.PuntoVenta:D4}-{comp.Numero:D8}.pdf",
                                pdfBytes, "application/pdf")]
                        ), ct);
                        emailEnviado = true;
                    }
                    catch (Exception ex) { errorEmail = ex.Message; }
                }
            }

            // Enviar WhatsApp
            if (dto.EnviarWhatsApp)
            {
                var telefono = dto.TelefonoDestino ?? cliente?.Telefono;
                if (string.IsNullOrEmpty(telefono))
                {
                    errorWhatsApp = "El cliente no tiene teléfono registrado.";
                }
                else
                {
                    try
                    {
                        var numeroFactura = $"{comp.PuntoVenta:D4}-{comp.Numero:D8}";
                        // URL pública del PDF (requiere HTTPS configurado en producción)
                        var urlPdf = $"/api/reportes/comprobantes/{comp.IdComprobante}/pdf";
                        await whatsAppService.EnviarComprobanteAsync(telefono, urlPdf, numeroFactura, ct);
                        whatsAppEnviado = true;
                    }
                    catch (Exception ex) { errorWhatsApp = ex.Message; }
                }
            }

            return Results.Ok(new ResultadoEnvioDto(emailEnviado, whatsAppEnviado, errorEmail, errorWhatsApp));
        })
        .WithName("EnviarComprobante")
        .Produces<ResultadoEnvioDto>()
        .ProducesProblem(404);

        return app;
    }
}

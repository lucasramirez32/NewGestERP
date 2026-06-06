using NewGest.Application.DTOs.Reportes;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace NewGest.Infrastructure.Reports;

/// <summary>
/// Genera el PDF del comprobante electrónico (FC-A, FC-B, FC-C, NC, ND).
/// Reemplaza FACELEC1.FRX, FACELEC2.FRX y variantes del sistema VFP.
/// Layout: encabezado empresa | datos comprobante | grilla ítems | totales | pie con CAE + QR
/// </summary>
public class ComprobanteDocument : IDocument
{
    private readonly ComprobanteReportDto _d;

    public ComprobanteDocument(ComprobanteReportDto datos) => _d = datos;

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(1.5f, Unit.Centimetre);
            page.DefaultTextStyle(x => x.FontSize(9).FontFamily(Fonts.Arial));

            page.Content().Column(col =>
            {
                col.Item().Row(row =>
                {
                    // Datos empresa (izquierda)
                    row.RelativeItem(3).Column(c =>
                    {
                        c.Item().Text(_d.NombreEmpresa).Bold().FontSize(14);
                        c.Item().Text(_d.RazonSocialEmpresa).FontSize(10);
                        c.Item().Text($"CUIT: {FormatCuit(_d.CuitEmpresa)}");
                        c.Item().Text($"Condición IVA: {_d.CondicionIvaEmpresa}");
                        c.Item().Text(_d.DomicilioEmpresa);
                    });

                    // Tipo y número (centro destacado)
                    row.RelativeItem(2).Border(1).BorderColor(Colors.Grey.Medium).Padding(8).Column(c =>
                    {
                        c.Item().AlignCenter().Text(_d.TipoLabel).Bold().FontSize(16);
                        c.Item().AlignCenter().Text($"Nº {_d.PuntoVenta:D4}-{_d.Numero:D8}").Bold().FontSize(12);
                        c.Item().AlignCenter().Text($"Fecha: {_d.Fecha:dd/MM/yyyy}");
                    });
                });

                col.Item().PaddingTop(10).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);

                // Datos receptor
                col.Item().PaddingTop(8).Table(t =>
                {
                    t.ColumnsDefinition(c => { c.RelativeColumn(2); c.RelativeColumn(2); c.RelativeColumn(1); });
                    t.Cell().Text("Señor(es):").SemiBold();
                    t.Cell().Text("CUIT:").SemiBold();
                    t.Cell().Text("Condición IVA:").SemiBold();
                    t.Cell().Text(_d.RazonSocialCliente);
                    t.Cell().Text(_d.CuitCliente is not null ? FormatCuit(_d.CuitCliente) : "Consumidor Final");
                    t.Cell().Text(_d.CondicionIvaCliente);
                });

                col.Item().PaddingTop(8).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);

                // Grilla de ítems
                col.Item().PaddingTop(8).Table(t =>
                {
                    t.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(4);
                        c.ConstantColumn(50);
                        c.ConstantColumn(70);
                        c.ConstantColumn(50);
                        c.ConstantColumn(70);
                    });

                    // Header
                    static IContainer CeldaHeader(IContainer c) =>
                        c.Background(Colors.Grey.Lighten3).Padding(4);

                    t.Header(h =>
                    {
                        h.Cell().Element(CeldaHeader).Text("Descripción").SemiBold();
                        h.Cell().Element(CeldaHeader).AlignRight().Text("Cant.").SemiBold();
                        h.Cell().Element(CeldaHeader).AlignRight().Text("P. Unit.").SemiBold();
                        h.Cell().Element(CeldaHeader).AlignCenter().Text("IVA").SemiBold();
                        h.Cell().Element(CeldaHeader).AlignRight().Text("Subtotal").SemiBold();
                    });

                    // Filas
                    foreach (var item in _d.Items)
                    {
                        t.Cell().Padding(3).Text(item.Descripcion);
                        t.Cell().Padding(3).AlignRight().Text($"{item.Cantidad:N2}");
                        t.Cell().Padding(3).AlignRight().Text($"${item.PrecioUnitario:N2}");
                        t.Cell().Padding(3).AlignCenter().Text(item.Alicuota);
                        t.Cell().Padding(3).AlignRight().Text($"${item.Subtotal:N2}");
                    }
                });

                col.Item().PaddingTop(8).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);

                // Totales (derecha) + QR (izquierda)
                col.Item().PaddingTop(6).Row(row =>
                {
                    // QR fiscal — Fix BUG-01: renderizar imagen PNG generada por IQrFiscalService
                    row.RelativeItem(2).Column(c =>
                    {
                        if (_d.CodigoCae is not null)
                        {
                            c.Item().Text("Comprobante Electrónico").SemiBold().FontSize(8);
                            c.Item().Text($"CAE: {_d.CodigoCae}").FontSize(8);
                            c.Item().Text($"Vence: {_d.VencimientoCae:dd/MM/yyyy}").FontSize(8);

                            if (_d.QrPngBytes is { Length: > 0 })
                                c.Item().Width(70).Height(70).Image(_d.QrPngBytes);
                        }
                    });

                    // Panel totales
                    row.RelativeItem(2).Column(c =>
                    {
                        foreach (var al in _d.Alicuotas)
                        {
                            c.Item().Row(r =>
                            {
                                r.RelativeItem().Text($"IVA {al.Porcentaje} (${al.Base:N2})");
                                r.ConstantItem(80).AlignRight().Text($"${al.Iva:N2}");
                            });
                        }
                        c.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
                        c.Item().Row(r =>
                        {
                            r.RelativeItem().Text("TOTAL").Bold().FontSize(12);
                            r.ConstantItem(80).AlignRight().Text($"${_d.Total:N2}").Bold().FontSize(12);
                        });
                    });
                });
            });

            page.Footer().AlignCenter().Text(x =>
            {
                x.Span("Página ").FontSize(8).FontColor(Colors.Grey.Medium);
                x.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Medium);
                x.Span(" de ").FontSize(8).FontColor(Colors.Grey.Medium);
                x.TotalPages().FontSize(8).FontColor(Colors.Grey.Medium);
            });
        });
    }

    private static string FormatCuit(string cuit)
    {
        var s = cuit.Replace("-", "").Replace(" ", "");
        return s.Length == 11 ? $"{s[..2]}-{s[2..10]}-{s[10]}" : cuit;
    }
}

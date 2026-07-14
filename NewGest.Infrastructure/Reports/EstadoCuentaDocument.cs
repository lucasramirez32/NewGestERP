using NewGest.Application.DTOs.Reportes;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace NewGest.Infrastructure.Reports;

/// <summary>
/// PDF de Estado de Cuenta del cliente.
/// Reemplaza EDO_CTA_FINAL.SCX y EDO_CTA_SINSALTO.SCX del sistema VFP.
/// </summary>
public class EstadoCuentaDocument : IDocument
{
    private readonly EstadoCuentaReportDto _d;

    public EstadoCuentaDocument(EstadoCuentaReportDto datos) => _d = datos;

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
                // Encabezado empresa + cliente
                col.Item().Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text(_d.NombreEmpresa).Bold().FontSize(13);
                        c.Item().Text($"CUIT: {_d.CuitEmpresa}");
                    });
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("ESTADO DE CUENTA").Bold().FontSize(13).AlignRight();
                        c.Item().Text($"Período: {_d.FechaDesde:dd/MM/yyyy} al {_d.FechaHasta:dd/MM/yyyy}").AlignRight();
                    });
                });

                col.Item().PaddingTop(6).Background(Colors.Grey.Lighten3).Padding(6).Column(c =>
                {
                    c.Item().Text($"Cliente: {_d.RazonSocialCliente}").SemiBold();
                    if (_d.CuitCliente is not null)
                        c.Item().Text($"CUIT: {_d.CuitCliente}");
                });

                col.Item().PaddingTop(8).Table(t =>
                {
                    t.ColumnsDefinition(c =>
                    {
                        c.ConstantColumn(60);   // Fecha
                        c.RelativeColumn(2);    // Comprobante
                        c.RelativeColumn(3);    // Descripción
                        c.ConstantColumn(75);   // Debe
                        c.ConstantColumn(75);   // Haber
                        c.ConstantColumn(80);   // Saldo
                    });

                    t.Header(h =>
                    {
                        static IContainer Hdr(IContainer c) =>
                            c.Background(Colors.Blue.Lighten4).Padding(3);
                        h.Cell().Element(Hdr).Text("Fecha").SemiBold();
                        h.Cell().Element(Hdr).Text("Comprobante").SemiBold();
                        h.Cell().Element(Hdr).Text("Descripción").SemiBold();
                        h.Cell().Element(Hdr).AlignRight().Text("Débito").SemiBold();
                        h.Cell().Element(Hdr).AlignRight().Text("Crédito").SemiBold();
                        h.Cell().Element(Hdr).AlignRight().Text("Saldo").SemiBold();
                    });

                    // Saldo anterior
                    t.Cell().ColumnSpan(5).Padding(2).Text("SALDO ANTERIOR").Italic();
                    t.Cell().Padding(2).AlignRight()
                        .Text($"${_d.SaldoAnterior:N2}").Bold();

                    foreach (var m in _d.Movimientos)
                    {
                        t.Cell().Padding(2).Text(m.Fecha.ToString("dd/MM/yy"));
                        t.Cell().Padding(2).Text(m.Comprobante);
                        t.Cell().Padding(2).Text(m.Descripcion);
                        t.Cell().Padding(2).AlignRight()
                            .Text(m.Debe > 0 ? $"${m.Debe:N2}" : "");
                        t.Cell().Padding(2).AlignRight()
                            .Text(m.Haber > 0 ? $"${m.Haber:N2}" : "");
                        t.Cell().Padding(2).AlignRight()
                            .Text($"${m.SaldoAcumulado:N2}")
                            .FontColor(m.SaldoAcumulado >= 0 ? Colors.Black : Colors.Red.Medium);
                    }

                    // Saldo final
                    static IContainer SaldoCell(IContainer c) =>
                        c.Background(Colors.Grey.Lighten3).BorderTop(1).BorderColor(Colors.Grey.Medium).Padding(3);
                    t.Cell().ColumnSpan(5).Element(SaldoCell).Text("SALDO FINAL").Bold();
                    t.Cell().Element(SaldoCell).AlignRight()
                        .Text($"${_d.SaldoFinal:N2}").Bold()
                        .FontColor(_d.SaldoFinal >= 0 ? Colors.Black : Colors.Red.Medium);
                });
            });

            page.Footer().AlignCenter().Text(x =>
            {
                x.Span("Página ").FontSize(7).FontColor(Colors.Grey.Medium);
                x.CurrentPageNumber().FontSize(7).FontColor(Colors.Grey.Medium);
                x.Span(" de ").FontSize(7).FontColor(Colors.Grey.Medium);
                x.TotalPages().FontSize(7).FontColor(Colors.Grey.Medium);
            });
        });
    }
}

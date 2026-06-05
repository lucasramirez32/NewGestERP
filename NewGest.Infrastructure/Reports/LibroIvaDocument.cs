using NewGest.Application.DTOs.Reportes;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace NewGest.Infrastructure.Reports;

/// <summary>
/// PDF del Libro IVA Ventas o Compras (regulatorio — RG AFIP 3685/2014).
/// Reemplaza los reportes LIBROIVA*.FRX del sistema VFP.
/// </summary>
public class LibroIvaDocument : IDocument
{
    private readonly LibroIvaReportDto _d;

    public LibroIvaDocument(LibroIvaReportDto datos) => _d = datos;

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(1.5f, Unit.Centimetre);
            page.DefaultTextStyle(x => x.FontSize(8).FontFamily(Fonts.Arial));

            page.Content().Column(col =>
            {
                // Encabezado
                col.Item().Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text($"Libro IVA {_d.TipoLibro}").Bold().FontSize(13);
                        c.Item().Text($"{_d.NombreEmpresa} — CUIT: {_d.CuitEmpresa}");
                        c.Item().Text($"Período: {_d.Mes:D2}/{_d.Anio}");
                    });
                });

                col.Item().PaddingTop(8).Table(t =>
                {
                    t.ColumnsDefinition(c =>
                    {
                        c.ConstantColumn(55);   // Fecha
                        c.RelativeColumn(2);    // Comprobante
                        c.RelativeColumn(3);    // Razón Social
                        c.ConstantColumn(85);   // CUIT
                        c.ConstantColumn(70);   // Neto
                        c.ConstantColumn(60);   // IVA
                        c.ConstantColumn(70);   // Total
                    });

                    t.Header(h =>
                    {
                        static IContainer Hdr(IContainer c) =>
                            c.Background(Colors.Grey.Lighten3).Padding(3);

                        h.Cell().Element(Hdr).Text("Fecha").SemiBold();
                        h.Cell().Element(Hdr).Text("Comprobante").SemiBold();
                        h.Cell().Element(Hdr).Text("Razón Social").SemiBold();
                        h.Cell().Element(Hdr).Text("CUIT").SemiBold();
                        h.Cell().Element(Hdr).AlignRight().Text("Neto").SemiBold();
                        h.Cell().Element(Hdr).AlignRight().Text("IVA").SemiBold();
                        h.Cell().Element(Hdr).AlignRight().Text("Total").SemiBold();
                    });

                    foreach (var l in _d.Lineas)
                    {
                        t.Cell().Padding(2).Text(l.Fecha.ToString("dd/MM/yy"));
                        t.Cell().Padding(2).Text(l.Comprobante);
                        t.Cell().Padding(2).Text(l.RazonSocial).Italic();
                        t.Cell().Padding(2).Text(l.Cuit ?? "");
                        t.Cell().Padding(2).AlignRight().Text($"{l.Neto:N2}");
                        t.Cell().Padding(2).AlignRight().Text($"{l.Iva:N2}");
                        t.Cell().Padding(2).AlignRight().Text($"{l.Total:N2}");
                    }

                    // Totales
                    static IContainer TotalCell(IContainer c) =>
                        c.Background(Colors.Grey.Lighten3).BorderTop(1).BorderColor(Colors.Grey.Medium).Padding(3);

                    t.Cell().ColumnSpan(4).Element(TotalCell).Text("TOTALES").Bold();
                    t.Cell().Element(TotalCell).AlignRight().Text($"{_d.TotalNeto:N2}").Bold();
                    t.Cell().Element(TotalCell).AlignRight().Text($"{_d.TotalIva:N2}").Bold();
                    t.Cell().Element(TotalCell).AlignRight().Text($"{_d.TotalGeneral:N2}").Bold();
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

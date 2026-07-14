using NewGest.Application.DTOs.Reportes;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace NewGest.Infrastructure.Reports;

/// <summary>
/// PDF del Libro IVA Ventas o Compras (regulatorio — RG AFIP 3685/2014).
/// Fix BUG-03: columnas desgloseadas por alícuota: Neto21/IVA21/Neto105/IVA105/Exento/Total.
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
            page.Margin(1.2f, Unit.Centimetre);
            page.DefaultTextStyle(x => x.FontSize(7.5f).FontFamily(Fonts.Arial));

            page.Content().Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text($"Libro IVA {_d.TipoLibro}").Bold().FontSize(12);
                        c.Item().Text($"{_d.NombreEmpresa} — CUIT: {_d.CuitEmpresa}");
                        c.Item().Text($"Período: {_d.Mes:D2}/{_d.Anio}");
                    });
                });

                col.Item().PaddingTop(8).Table(t =>
                {
                    t.ColumnsDefinition(c =>
                    {
                        c.ConstantColumn(48);   // Fecha
                        c.RelativeColumn(2);    // Comprobante
                        c.RelativeColumn(3);    // Razón Social
                        c.ConstantColumn(78);   // CUIT
                        c.ConstantColumn(58);   // Neto 21%
                        c.ConstantColumn(52);   // IVA 21%
                        c.ConstantColumn(58);   // Neto 10,5%
                        c.ConstantColumn(52);   // IVA 10,5%
                        c.ConstantColumn(52);   // Exento
                        c.ConstantColumn(62);   // Total
                    });

                    t.Header(h =>
                    {
                        static IContainer Hdr(IContainer c) =>
                            c.Background(Colors.Grey.Lighten3).Padding(2);

                        h.Cell().Element(Hdr).Text("Fecha").SemiBold();
                        h.Cell().Element(Hdr).Text("Comprobante").SemiBold();
                        h.Cell().Element(Hdr).Text("Razón Social").SemiBold();
                        h.Cell().Element(Hdr).Text("CUIT").SemiBold();
                        h.Cell().Element(Hdr).AlignRight().Text("Neto 21%").SemiBold();
                        h.Cell().Element(Hdr).AlignRight().Text("IVA 21%").SemiBold();
                        h.Cell().Element(Hdr).AlignRight().Text("Neto 10,5%").SemiBold();
                        h.Cell().Element(Hdr).AlignRight().Text("IVA 10,5%").SemiBold();
                        h.Cell().Element(Hdr).AlignRight().Text("Exento").SemiBold();
                        h.Cell().Element(Hdr).AlignRight().Text("Total").SemiBold();
                    });

                    foreach (var l in _d.Lineas)
                    {
                        t.Cell().Padding(2).Text(l.Fecha.ToString("dd/MM/yy"));
                        t.Cell().Padding(2).Text(l.Comprobante);
                        t.Cell().Padding(2).Text(l.RazonSocial);
                        t.Cell().Padding(2).Text(l.Cuit ?? "");
                        t.Cell().Padding(2).AlignRight().Text(l.Neto21 > 0 ? $"{l.Neto21:N2}" : "");
                        t.Cell().Padding(2).AlignRight().Text(l.Iva21  > 0 ? $"{l.Iva21:N2}"  : "");
                        t.Cell().Padding(2).AlignRight().Text(l.Neto105 > 0 ? $"{l.Neto105:N2}" : "");
                        t.Cell().Padding(2).AlignRight().Text(l.Iva105  > 0 ? $"{l.Iva105:N2}"  : "");
                        t.Cell().Padding(2).AlignRight().Text(l.Exento  > 0 ? $"{l.Exento:N2}"  : "");
                        t.Cell().Padding(2).AlignRight().Text($"{l.Total:N2}").Bold();
                    }

                    static IContainer TotalCell(IContainer c) =>
                        c.Background(Colors.Grey.Lighten3).BorderTop(1).BorderColor(Colors.Grey.Medium).Padding(2);

                    t.Cell().ColumnSpan(4).Element(TotalCell).Text("TOTALES").Bold();
                    t.Cell().Element(TotalCell).AlignRight().Text($"{_d.TotalNeto21:N2}").Bold();
                    t.Cell().Element(TotalCell).AlignRight().Text($"{_d.TotalIva21:N2}").Bold();
                    t.Cell().Element(TotalCell).AlignRight().Text($"{_d.TotalNeto105:N2}").Bold();
                    t.Cell().Element(TotalCell).AlignRight().Text($"{_d.TotalIva105:N2}").Bold();
                    t.Cell().Element(TotalCell).AlignRight().Text($"{_d.TotalExento:N2}").Bold();
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

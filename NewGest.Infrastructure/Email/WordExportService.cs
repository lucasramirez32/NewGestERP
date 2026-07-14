using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using NewGest.Application.Services;

namespace NewGest.Infrastructure.Email;

/// <summary>
/// Reemplaza EXPORDOC.SCX y expor1doc.prg del sistema VFP (usaban COM Word).
/// DocumentFormat.OpenXml no requiere Office instalado en el servidor.
/// Usa archivo temporal para garantizar flush completo del ZIP interno.
/// </summary>
public class WordExportService : IWordExportService
{
    public byte[] ExportarInformeContable(InformeContableDto datos)
    {
        var tmpPath = Path.Combine(Path.GetTempPath(), $"ng_{Guid.NewGuid():N}.docx");
        try
        {
            using (var doc = WordprocessingDocument.Create(tmpPath, WordprocessingDocumentType.Document))
            {
                var main = doc.AddMainDocumentPart();
                main.Document = new Document(new Body());
                var body = main.Document.Body!;

                body.AppendChild(Parrafo(datos.Titulo, bold: true, fontSize: 28));
                body.AppendChild(Parrafo($"{datos.NombreEmpresa} — {datos.Periodo}", bold: false, fontSize: 22));
                body.AppendChild(new Paragraph());

                foreach (var seccion in datos.Secciones)
                {
                    body.AppendChild(Parrafo(seccion.Titulo, bold: true, fontSize: 24));

                    var tabla = new Table();
                    tabla.AppendChild(FilaTablaEncabezado("Concepto", "Debe", "Haber", "Saldo"));

                    foreach (var fila in seccion.Filas)
                        tabla.AppendChild(FilaTabla(fila.Concepto,
                            fila.Debe  > 0 ? $"${fila.Debe:N2}"  : "",
                            fila.Haber > 0 ? $"${fila.Haber:N2}" : "",
                            $"${fila.Saldo:N2}"));

                    body.AppendChild(tabla);
                    body.AppendChild(new Paragraph());
                }

                main.Document.Save();
            }
            return File.ReadAllBytes(tmpPath);
        }
        finally { if (File.Exists(tmpPath)) File.Delete(tmpPath); }
    }

    public byte[] ExportarEstadoCuenta(string nombreCliente, string periodo, string contenidoHtml)
    {
        var tmpPath = Path.Combine(Path.GetTempPath(), $"ng_{Guid.NewGuid():N}.docx");
        try
        {
            using (var doc = WordprocessingDocument.Create(tmpPath, WordprocessingDocumentType.Document))
            {
                var main = doc.AddMainDocumentPart();
                main.Document = new Document(new Body());
                var body = main.Document.Body!;

                body.AppendChild(Parrafo($"Estado de Cuenta — {nombreCliente}", bold: true, fontSize: 28));
                body.AppendChild(Parrafo(periodo, bold: false, fontSize: 22));
                body.AppendChild(new Paragraph());

                var textoPlano = System.Text.RegularExpressions.Regex.Replace(contenidoHtml, "<[^>]*>", "");
                foreach (var linea in textoPlano.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                    body.AppendChild(Parrafo(linea.Trim(), bold: false, fontSize: 20));

                main.Document.Save();
            }
            return File.ReadAllBytes(tmpPath);
        }
        finally { if (File.Exists(tmpPath)) File.Delete(tmpPath); }
    }

    private static Paragraph Parrafo(string texto, bool bold, int fontSize)
    {
        var props = new RunProperties(new FontSize { Val = fontSize.ToString() });
        if (bold) props.AppendChild(new Bold());
        return new Paragraph(new Run(props, new Text(texto)));
    }

    private static TableRow FilaTablaEncabezado(params string[] celdas)
    {
        var fila = new TableRow();
        foreach (var c in celdas)
        {
            var celda = new TableCell(new Paragraph(
                new Run(new RunProperties(new Bold()), new Text(c))));
            celda.PrependChild(new TableCellProperties(
                new Shading { Val = ShadingPatternValues.Clear, Fill = "D9E1F2" }));
            fila.AppendChild(celda);
        }
        return fila;
    }

    private static TableRow FilaTabla(params string[] celdas)
    {
        var fila = new TableRow();
        foreach (var c in celdas)
            fila.AppendChild(new TableCell(new Paragraph(new Run(new Text(c)))));
        return fila;
    }
}

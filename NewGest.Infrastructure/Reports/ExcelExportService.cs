using ClosedXML.Excel;
using NewGest.Application.DTOs.Reportes;
using NewGest.Application.Services;

namespace NewGest.Infrastructure.Reports;

public class ExcelExportService : IExcelExportService
{
    public byte[] ExportarClientes(IEnumerable<ClienteExportDto> datos)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Clientes");

        string[] headers = ["Código", "Razón Social", "CUIT", "Condición IVA", "Localidad", "Teléfono", "Email"];
        for (var i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];
        EstilarEncabezado(ws.Range(1, 1, 1, headers.Length));

        var row = 2;
        foreach (var c in datos)
        {
            ws.Cell(row, 1).Value = c.Codigo;
            ws.Cell(row, 2).Value = c.RazonSocial;
            ws.Cell(row, 3).Value = c.Cuit ?? "";
            ws.Cell(row, 4).Value = c.CondicionIva;
            ws.Cell(row, 5).Value = c.Localidad ?? "";
            ws.Cell(row, 6).Value = c.Telefono ?? "";
            ws.Cell(row, 7).Value = c.Email ?? "";
            row++;
        }

        ws.Columns().AdjustToContents();
        return ToBytes(wb);
    }

    public byte[] ExportarArticulos(IEnumerable<ArticuloExportDto> datos)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Artículos");

        string[] headers = ["Código", "Descripción", "Grupo", "Unidad", "Precio Venta", "Activo"];
        for (var i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];
        EstilarEncabezado(ws.Range(1, 1, 1, headers.Length));

        var row = 2;
        foreach (var a in datos)
        {
            ws.Cell(row, 1).Value = a.Codigo;
            ws.Cell(row, 2).Value = a.Descripcion;
            ws.Cell(row, 3).Value = a.Grupo;
            ws.Cell(row, 4).Value = a.Unidad;
            ws.Cell(row, 5).Value = a.PrecioVenta;
            ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 6).Value = a.Activo ? "Sí" : "No";
            row++;
        }

        ws.Columns().AdjustToContents();
        return ToBytes(wb);
    }

    public byte[] ExportarMovimientosStock(IEnumerable<MovimientoExportDto> datos)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Movimientos Stock");

        string[] headers = ["Fecha", "Artículo", "Depósito", "Tipo", "Cantidad", "Costo Unit."];
        for (var i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];
        EstilarEncabezado(ws.Range(1, 1, 1, headers.Length));

        var row = 2;
        foreach (var m in datos)
        {
            ws.Cell(row, 1).Value = m.Fecha.ToDateTime(TimeOnly.MinValue);
            ws.Cell(row, 1).Style.DateFormat.Format = "dd/mm/yyyy";
            ws.Cell(row, 2).Value = m.Articulo;
            ws.Cell(row, 3).Value = m.Deposito;
            ws.Cell(row, 4).Value = m.Tipo;
            ws.Cell(row, 5).Value = m.Cantidad;
            ws.Cell(row, 6).Value = m.CostoUnitario;
            ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0.0000";
            row++;
        }

        ws.Columns().AdjustToContents();
        return ToBytes(wb);
    }

    public byte[] ExportarLibroIva(LibroIvaReportDto datos)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add($"Libro IVA {datos.TipoLibro}");

        // Título
        ws.Cell(1, 1).Value = $"Libro IVA {datos.TipoLibro} — {datos.NombreEmpresa}";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 12;
        ws.Cell(2, 1).Value = $"CUIT: {datos.CuitEmpresa}  |  Período: {datos.Mes:D2}/{datos.Anio}";

        string[] headers = ["Fecha", "Comprobante", "Razón Social", "CUIT", "Neto", "IVA", "Total"];
        for (var i = 0; i < headers.Length; i++)
            ws.Cell(4, i + 1).Value = headers[i];
        EstilarEncabezado(ws.Range(4, 1, 4, headers.Length));

        var row = 5;
        foreach (var l in datos.Lineas)
        {
            ws.Cell(row, 1).Value = l.Fecha.ToDateTime(TimeOnly.MinValue);
            ws.Cell(row, 1).Style.DateFormat.Format = "dd/mm/yyyy";
            ws.Cell(row, 2).Value = l.Comprobante;
            ws.Cell(row, 3).Value = l.RazonSocial;
            ws.Cell(row, 4).Value = l.Cuit ?? "";
            ws.Cell(row, 5).Value = l.Neto;
            ws.Cell(row, 6).Value = l.Iva;
            ws.Cell(row, 7).Value = l.Total;
            foreach (var col in new[] { 5, 6, 7 })
                ws.Cell(row, col).Style.NumberFormat.Format = "#,##0.00";
            row++;
        }

        // Totales
        ws.Cell(row, 4).Value = "TOTALES";
        ws.Cell(row, 4).Style.Font.Bold = true;
        ws.Cell(row, 5).Value = datos.TotalNeto;
        ws.Cell(row, 6).Value = datos.TotalIva;
        ws.Cell(row, 7).Value = datos.TotalGeneral;
        foreach (var col in new[] { 5, 6, 7 })
        {
            ws.Cell(row, col).Style.Font.Bold = true;
            ws.Cell(row, col).Style.NumberFormat.Format = "#,##0.00";
        }

        ws.Columns().AdjustToContents();
        return ToBytes(wb);
    }

    private static void EstilarEncabezado(IXLRange rango)
    {
        rango.Style.Font.Bold = true;
        rango.Style.Fill.BackgroundColor = XLColor.FromHtml("#4472C4");
        rango.Style.Font.FontColor = XLColor.White;
        rango.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
    }

    private static byte[] ToBytes(XLWorkbook wb)
    {
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}

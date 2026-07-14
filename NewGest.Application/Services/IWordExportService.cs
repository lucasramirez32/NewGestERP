namespace NewGest.Application.Services;

public interface IWordExportService
{
    byte[] ExportarInformeContable(InformeContableDto datos);
    byte[] ExportarEstadoCuenta(string nombreCliente, string periodo, string contenidoHtml);
}

public record InformeContableDto(
    string NombreEmpresa,
    string Periodo,
    string Titulo,
    IReadOnlyList<SeccionInformeDto> Secciones
);

public record SeccionInformeDto(string Titulo, IReadOnlyList<FilaInformeDto> Filas);
public record FilaInformeDto(string Concepto, decimal Debe, decimal Haber, decimal Saldo);

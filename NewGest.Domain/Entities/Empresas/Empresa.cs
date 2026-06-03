namespace NewGest.Domain.Entities.Empresas;

/// <summary>
/// Empresa registrada en el sistema.
/// Reemplaza los directorios EM###### del sistema VFP.
/// </summary>
public class Empresa
{
    public int IdEmpresa { get; set; }
    public string Nombre { get; set; } = default!;
    public string? RazonSocial { get; set; }
    public string? Cuit { get; set; }
    public bool Activa { get; set; } = true;
}

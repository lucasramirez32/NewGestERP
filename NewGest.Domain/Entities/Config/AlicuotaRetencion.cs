namespace NewGest.Domain.Entities.Config;

public class AlicuotaRetencion
{
    public int IdAlicuota { get; set; }
    public int IdEmpresa { get; set; }
    public string TipoRetencion { get; set; } = default!;
    public string? Provincia { get; set; }
    public decimal Porcentaje { get; set; }
    public DateOnly VigenciaDesde { get; set; }
    public DateOnly? VigenciaHasta { get; set; }
}

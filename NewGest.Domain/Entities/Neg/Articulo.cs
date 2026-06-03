using NewGest.Domain.Common;

namespace NewGest.Domain.Entities.Neg;

public class Articulo
{
    public int IdArticulo { get; private set; }
    public int IdEmpresa { get; private set; }
    public string Codigo { get; private set; } = default!;
    public string Descripcion { get; private set; } = default!;
    public int IdGrupo { get; private set; }
    public GrupoArticulo? Grupo { get; private set; }
    public int IdUnidad { get; private set; }
    public Unidad? Unidad { get; private set; }
    public decimal PrecioLista { get; private set; }
    public decimal PrecioCosto { get; private set; }
    public decimal PorcentajeIva { get; private set; }
    public bool Activo { get; private set; } = true;
    public string? Observaciones { get; private set; }

    private static readonly decimal[] IvaPermitidos = [0m, 10.5m, 21m];

    private Articulo() { }

    public static Articulo Crear(
        int idEmpresa,
        string codigo,
        string descripcion,
        int idGrupo,
        int idUnidad,
        decimal precioLista,
        decimal precioCosto,
        decimal porcentajeIva,
        string? observaciones)
    {
        if (precioLista < 0) throw new DomainException("El precio de lista no puede ser negativo.");
        if (precioCosto < 0) throw new DomainException("El precio de costo no puede ser negativo.");
        if (!IvaPermitidos.Contains(porcentajeIva))
            throw new DomainException($"Porcentaje de IVA inválido: {porcentajeIva}. Valores aceptados: 0, 10.5, 21.");

        return new Articulo
        {
            IdEmpresa = idEmpresa,
            Codigo = codigo.Trim().ToUpper(),
            Descripcion = descripcion.Trim(),
            IdGrupo = idGrupo,
            IdUnidad = idUnidad,
            PrecioLista = precioLista,
            PrecioCosto = precioCosto,
            PorcentajeIva = porcentajeIva,
            Observaciones = observaciones,
            Activo = true
        };
    }

    public void Actualizar(
        string descripcion,
        int idGrupo,
        int idUnidad,
        decimal precioLista,
        decimal precioCosto,
        decimal porcentajeIva,
        string? observaciones)
    {
        if (precioLista < 0) throw new DomainException("El precio de lista no puede ser negativo.");
        if (precioCosto < 0) throw new DomainException("El precio de costo no puede ser negativo.");
        if (!IvaPermitidos.Contains(porcentajeIva))
            throw new DomainException($"Porcentaje de IVA inválido: {porcentajeIva}. Valores aceptados: 0, 10.5, 21.");

        Descripcion = descripcion.Trim();
        IdGrupo = idGrupo;
        IdUnidad = idUnidad;
        PrecioLista = precioLista;
        PrecioCosto = precioCosto;
        PorcentajeIva = porcentajeIva;
        Observaciones = observaciones;
    }

    public void Desactivar() => Activo = false;
}

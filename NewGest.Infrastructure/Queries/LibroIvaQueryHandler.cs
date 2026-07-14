using Dapper;
using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NewGest.Application.DTOs.Contabilidad;
using NewGest.Application.Features.Contabilidad.Queries.GetLibroIva;
using NewGest.Infrastructure.Data;

namespace NewGest.Infrastructure.Queries;

public class LibroIvaQueryHandler : IRequestHandler<GetLibroIvaQuery, LibroIvaDto>
{
    private readonly NewgestDbContext _db;

    public LibroIvaQueryHandler(NewgestDbContext db) => _db = db;

    private const string SQL = """
        SELECT
            c.Fecha,
            c.Tipo           AS TipoComprobante,
            c.PuntoVenta,
            c.Numero,
            c.RazonSocialCliente AS RazonSocial,
            c.CuitCliente    AS Cuit,
            c.TotalNeto,
            c.TotalIva,
            c.Total
        FROM com.Comprobantes c
        WHERE c.IdEmpresa = @IdEmpresa
          AND YEAR(c.Fecha) = @Anio
          AND MONTH(c.Fecha) = @Mes
          AND c.Anulado = 0
          AND c.Tipo IN @TiposIncluidos
        ORDER BY c.Fecha, c.PuntoVenta, c.Numero
        """;

    // Tipos AFIP para Libro IVA Ventas (RG 3685/2014)
    // FC-A=1, FC-B=6, FC-C=11, FC-M=51, NC-A=3, NC-B=8, NC-C=13, ND-A=2, ND-B=7, ND-C=12
    private static readonly int[] TiposVentas = [1, 6, 11, 51, 3, 8, 13, 2, 7, 12];

    // Tipos AFIP para Libro IVA Compras (RG 3685/2014)
    // FC-A=41, FC-B=42, FC-C=43, FC-M=61, NC-A=44, NC-B=46, NC-C=48, ND-A=45, ND-B=47, ND-C=49
    // Liquidaciones=63, Despachos de importación=66
    private static readonly int[] TiposCompras = [41, 42, 43, 61, 44, 46, 48, 45, 47, 49, 63, 66];

    public async Task<LibroIvaDto> Handle(GetLibroIvaQuery request, CancellationToken ct)
    {
        var tipos = request.TipoLibro == TipoLibroIva.Ventas ? TiposVentas : TiposCompras;

        var connStr = _db.Database.GetConnectionString()!;
        await using var conn = new SqlConnection(connStr);

        var rows = await conn.QueryAsync<LibroIvaRow>(SQL, new
        {
            request.IdEmpresa,
            request.Anio,
            request.Mes,
            TiposIncluidos = tipos
        });

        var lineas = rows.Select(r => new LineaLibroIvaDto(
            r.Fecha, r.TipoComprobante.ToString(), r.PuntoVenta, r.Numero,
            r.RazonSocial, r.Cuit, r.TotalNeto, r.TotalIva, r.Total
        )).ToList();

        return new LibroIvaDto(
            request.Anio, request.Mes,
            request.TipoLibro.ToString(),
            lineas,
            lineas.Sum(l => l.TotalNeto),
            lineas.Sum(l => l.TotalIva),
            lineas.Sum(l => l.Total));
    }

    private record LibroIvaRow(
        DateOnly Fecha, int TipoComprobante, int PuntoVenta, long Numero,
        string RazonSocial, string? Cuit, decimal TotalNeto, decimal TotalIva, decimal Total);
}

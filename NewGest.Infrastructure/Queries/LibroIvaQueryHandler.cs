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

    // Tipos de comprobante que van al Libro IVA Ventas
    private static readonly int[] TiposVentas = [1, 6, 11, 51, 3, 8, 13, 2, 7, 12];

    public async Task<LibroIvaDto> Handle(GetLibroIvaQuery request, CancellationToken ct)
    {
        var connStr = _db.Database.GetConnectionString()!;
        await using var conn = new SqlConnection(connStr);

        var rows = await conn.QueryAsync<LibroIvaRow>(SQL, new
        {
            request.IdEmpresa,
            request.Anio,
            request.Mes,
            TiposIncluidos = TiposVentas
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

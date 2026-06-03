using Dapper;
using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using NewGest.Application.Features.Stock.Queries.GetStockValorizado;

namespace NewGest.Infrastructure.Queries;

public class GetStockValorizadoQueryHandler : IRequestHandler<GetStockValorizadoQuery, IReadOnlyList<StockValorizadoItemDto>>
{
    private readonly string _connectionString;

    public GetStockValorizadoQueryHandler(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("NewgestDb")
            ?? throw new InvalidOperationException("ConnectionString NewgestDb no configurada.");
    }

    public async Task<IReadOnlyList<StockValorizadoItemDto>> Handle(
        GetStockValorizadoQuery request, CancellationToken ct)
    {
        const string sql = """
            SELECT
                e.IdArticulo,
                a.Codigo    AS CodigoArticulo,
                a.Descripcion AS DescripcionArticulo,
                e.IdDeposito,
                d.Descripcion AS NombreDeposito,
                e.Cantidad,
                e.CostoPromedio,
                e.Cantidad * e.CostoPromedio AS ValorTotal
            FROM inv.ExistenciasDeposito e
            INNER JOIN neg.Articulos  a ON a.IdArticulo = e.IdArticulo AND a.Activo = 1
            INNER JOIN inv.Depositos  d ON d.IdDeposito = e.IdDeposito AND d.Activo = 1
            WHERE e.IdEmpresa = @IdEmpresa
              AND e.Cantidad  > 0
            ORDER BY a.Descripcion, d.Descripcion
            """;

        await using var conn = new SqlConnection(_connectionString);
        var rows = await conn.QueryAsync<StockValorizadoItemDto>(sql, new { request.IdEmpresa });
        return rows.ToList();
    }
}

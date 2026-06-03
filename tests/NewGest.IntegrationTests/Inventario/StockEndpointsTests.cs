using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NewGest.Application.DTOs.Shared;
using NewGest.Application.DTOs.Stock;
using NewGest.Domain.Enums;
using NewGest.Infrastructure.Data;

namespace NewGest.IntegrationTests.Inventario;

/// <summary>
/// Tests de integración para los endpoints de Stock y Depósitos (Sprint 5-7 — M09).
/// Verifica movimientos de entrada/salida/ajuste, existencias, alertas de reposición
/// y la ausencia de inconsistencias bajo carga concurrente.
/// </summary>
[Collection("IntegrationTests")]
public class StockEndpointsTests : IAsyncLifetime
{
    private readonly NewgestWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private string _token = default!;

    // IDs creados durante el seeding de este módulo
    private int _idArticuloTest;
    private int _idDepositoTest;

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    // Prefijos usados para identificar y limpiar datos de prueba
    private const string PrefixDeposito = "DEP-T-";
    private const string PrefixArticulo = "STK-T-";

    public StockEndpointsTests(NewgestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IAsyncLifetime — seeding y cleanup específicos de Stock
    // ──────────────────────────────────────────────────────────────────────────

    public async Task InitializeAsync()
    {
        _token = await LoginYObtenerTokenAsync();
        await LimpiarDatosStockAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NewgestDbContext>();

        // Crear depósito de prueba directamente en BD
        var deposito = new NewGest.Domain.Entities.Inv.Deposito
        {
            IdEmpresa = NewgestWebApplicationFactory.IdEmpresaTest,
            Codigo = "DEP-T-01  ",   // CHAR(10)
            Descripcion = "Depósito Test Stock",
            Activo = true
        };
        db.Depositos.Add(deposito);
        await db.SaveChangesAsync();
        _idDepositoTest = deposito.IdDeposito;

        // Necesitamos un grupo y unidad para crear artículo
        var grupo = new NewGest.Domain.Entities.Neg.GrupoArticulo
        {
            IdEmpresa = NewgestWebApplicationFactory.IdEmpresaTest,
            Descripcion = "STK-GRP-TEST",
            IdGrupoPadre = null
        };
        db.GruposArticulos.Add(grupo);
        await db.SaveChangesAsync();

        var articulo = NewGest.Domain.Entities.Neg.Articulo.Crear(
            NewgestWebApplicationFactory.IdEmpresaTest,
            "STK-T-001",
            "Artículo Test Stock",
            grupo.IdGrupo,
            idUnidad: 1,   // Unidad semilla de migración
            precioLista: 500m,
            precioCosto: 300m,
            porcentajeIva: 21m,
            observaciones: null);

        db.Articulos.Add(articulo);
        await db.SaveChangesAsync();
        _idArticuloTest = articulo.IdArticulo;
    }

    public async Task DisposeAsync()
    {
        await LimpiarDatosStockAsync();
    }

    private async Task LimpiarDatosStockAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NewgestDbContext>();

        // Limpiar en orden FK: movimientos → existencias → artículos → depósitos → grupos
        await db.Database.ExecuteSqlRawAsync(
            "DELETE FROM inv.MovimientosStock WHERE IdEmpresa = 1 AND IdArticulo IN (SELECT IdArticulo FROM neg.Articulos WHERE Codigo LIKE 'STK-T-%')");
        await db.Database.ExecuteSqlRawAsync(
            "DELETE FROM inv.ExistenciasDeposito WHERE IdEmpresa = 1 AND IdArticulo IN (SELECT IdArticulo FROM neg.Articulos WHERE Codigo LIKE 'STK-T-%')");
        await db.Database.ExecuteSqlRawAsync(
            "DELETE FROM neg.Articulos WHERE Codigo LIKE 'STK-T-%' AND IdEmpresa = 1");
        await db.Database.ExecuteSqlRawAsync(
            "DELETE FROM inv.Depositos WHERE Codigo LIKE 'DEP-T-%' AND IdEmpresa = 1");
        await db.Database.ExecuteSqlRawAsync(
            "DELETE FROM neg.GruposArticulos WHERE Descripcion LIKE 'STK-GRP-%' AND IdEmpresa = 1");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GET /api/depositos
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GET_depositos_sin_jwt_retorna_401()
    {
        var response = await _client.GetAsync("/api/depositos");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GET_depositos_con_jwt_retorna_200_y_lista_con_deposito_prueba()
    {
        using var client = ClienteConToken();

        var response = await client.GetAsync("/api/depositos");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var lista = await response.Content.ReadFromJsonAsync<List<DepositoDto>>(JsonOpts);
        lista.Should().NotBeNull();
        lista!.Should().Contain(d => d.IdDeposito == _idDepositoTest,
            "el depósito de prueba debe aparecer en la lista");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // POST /api/depositos
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task POST_deposito_sin_jwt_retorna_401()
    {
        var dto = new CrearDepositoDto("DEP-T-NJ", "Sin JWT");

        var response = await _client.PostAsJsonAsync("/api/depositos", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task POST_deposito_datos_validos_retorna_201_y_existe_en_BD()
    {
        using var client = ClienteConToken();
        var dto = new CrearDepositoDto("DEP-T-02", "Depósito Test Nuevo");

        var response = await client.PostAsJsonAsync("/api/depositos", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var resultado = await response.Content.ReadFromJsonAsync<DepositoDto>(JsonOpts);
        resultado.Should().NotBeNull();
        resultado!.IdDeposito.Should().BeGreaterThan(0);
        resultado.Activo.Should().BeTrue();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // POST /api/stock/movimientos — Entrada
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task POST_movimiento_entrada_sin_jwt_retorna_401()
    {
        var dto = new RegistrarMovimientoDto(_idArticuloTest, _idDepositoTest,
            TipoMovimiento.Entrada, 10m, 100m, null, null, null);

        var response = await _client.PostAsJsonAsync("/api/stock/movimientos", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task POST_movimiento_entrada_cantidad_cero_retorna_error()
    {
        using var client = ClienteConToken();
        var dto = new RegistrarMovimientoDto(_idArticuloTest, _idDepositoTest,
            TipoMovimiento.Entrada, 0m, 100m, null, null, null);

        var response = await client.PostAsJsonAsync("/api/stock/movimientos", dto);

        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity },
            "cantidad = 0 debe ser rechazada por DomainException");
    }

    [Fact]
    public async Task POST_movimiento_entrada_costo_negativo_retorna_error()
    {
        using var client = ClienteConToken();
        var dto = new RegistrarMovimientoDto(_idArticuloTest, _idDepositoTest,
            TipoMovimiento.Entrada, 5m, -1m, null, null, null);

        var response = await client.PostAsJsonAsync("/api/stock/movimientos", dto);

        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity },
            "costo negativo debe ser rechazado por DomainException");
    }

    [Fact]
    public async Task POST_movimiento_entrada_valido_retorna_201_y_actualiza_existencia()
    {
        using var client = ClienteConToken();
        var dto = new RegistrarMovimientoDto(_idArticuloTest, _idDepositoTest,
            TipoMovimiento.Entrada, 20m, 150m, null, null, "Entrada de prueba");

        var response = await client.PostAsJsonAsync("/api/stock/movimientos", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created,
            "un movimiento válido de entrada debe retornar 201");

        var resultado = await response.Content.ReadFromJsonAsync<MovimientoStockDto>(JsonOpts);
        resultado.Should().NotBeNull();
        resultado!.IdMovimiento.Should().BeGreaterThan(0);
        resultado.Tipo.Should().Be(TipoMovimiento.Entrada);
        resultado.Cantidad.Should().Be(20m);

        // Verificar que la existencia fue actualizada en BD
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NewgestDbContext>();
        var existencia = await db.ExistenciasDeposito
            .FirstOrDefaultAsync(e => e.IdArticulo == _idArticuloTest && e.IdDeposito == _idDepositoTest);

        existencia.Should().NotBeNull("la entrada debe crear/actualizar la existencia");
        existencia!.Cantidad.Should().BeGreaterThanOrEqualTo(20m,
            "la cantidad debe reflejar la entrada registrada");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // POST /api/stock/movimientos — Salida
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task POST_movimiento_salida_valido_retorna_201_y_disminuye_existencia()
    {
        using var client = ClienteConToken();

        // Primero cargar stock
        var entrada = new RegistrarMovimientoDto(_idArticuloTest, _idDepositoTest,
            TipoMovimiento.Entrada, 50m, 100m, null, null, null);
        var entradaResp = await client.PostAsJsonAsync("/api/stock/movimientos", entrada);
        entradaResp.StatusCode.Should().Be(HttpStatusCode.Created);

        // Obtener existencia previa para comparar
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NewgestDbContext>();
        var existenciaAntes = await db.ExistenciasDeposito
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.IdArticulo == _idArticuloTest && e.IdDeposito == _idDepositoTest);
        existenciaAntes.Should().NotBeNull();
        var cantidadAntes = existenciaAntes!.Cantidad;

        // Registrar salida
        var salida = new RegistrarMovimientoDto(_idArticuloTest, _idDepositoTest,
            TipoMovimiento.Salida, 15m, 100m, null, null, "Salida de prueba");
        var salidaResp = await client.PostAsJsonAsync("/api/stock/movimientos", salida);

        salidaResp.StatusCode.Should().Be(HttpStatusCode.Created);

        // Verificar que la existencia disminuyó
        var existenciaDespues = await db.ExistenciasDeposito
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.IdArticulo == _idArticuloTest && e.IdDeposito == _idDepositoTest);

        existenciaDespues!.Cantidad.Should().Be(cantidadAntes - 15m,
            "la salida debe disminuir la existencia exactamente en 15 unidades");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // POST /api/stock/movimientos — Ajuste
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task POST_movimiento_ajuste_valido_retorna_201_y_existencia_queda_en_valor_exacto()
    {
        using var client = ClienteConToken();

        // Cargar stock base
        var entrada = new RegistrarMovimientoDto(_idArticuloTest, _idDepositoTest,
            TipoMovimiento.Entrada, 30m, 100m, null, null, null);
        await client.PostAsJsonAsync("/api/stock/movimientos", entrada);

        // Ajuste a 25 unidades
        var ajuste = new RegistrarMovimientoDto(_idArticuloTest, _idDepositoTest,
            TipoMovimiento.Ajuste, 25m, 100m, null, null, "Ajuste de inventario");
        var ajusteResp = await client.PostAsJsonAsync("/api/stock/movimientos", ajuste);

        ajusteResp.StatusCode.Should().Be(HttpStatusCode.Created,
            "un ajuste válido debe retornar 201");

        // Verificar que la existencia quedó en 25 (valor absoluto del ajuste)
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NewgestDbContext>();
        var existencia = await db.ExistenciasDeposito
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.IdArticulo == _idArticuloTest && e.IdDeposito == _idDepositoTest);

        existencia!.Cantidad.Should().Be(25m,
            "el ajuste debe fijar la existencia al valor absoluto indicado");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GET /api/stock/{idArticulo}/existencias
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GET_existencias_sin_jwt_retorna_401()
    {
        var response = await _client.GetAsync($"/api/stock/{_idArticuloTest}/existencias");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GET_existencias_articulo_con_movimientos_retorna_200_y_cantidad_correcta()
    {
        using var client = ClienteConToken();

        // Registrar entrada conocida
        var dto = new RegistrarMovimientoDto(_idArticuloTest, _idDepositoTest,
            TipoMovimiento.Entrada, 10m, 200m, null, null, null);
        await client.PostAsJsonAsync("/api/stock/movimientos", dto);

        var response = await client.GetAsync($"/api/stock/{_idArticuloTest}/existencias");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var existencias = await response.Content.ReadFromJsonAsync<List<ExistenciaDepositoDto>>(JsonOpts);
        existencias.Should().NotBeNull();
        existencias!.Should().NotBeEmpty("debe haber existencia después de una entrada");
        existencias.Should().Contain(e => e.IdArticulo == _idArticuloTest,
            "la existencia debe corresponder al artículo consultado");
    }

    [Fact]
    public async Task GET_existencias_articulo_sin_movimientos_retorna_200_y_lista_vacia()
    {
        // Crear un artículo nuevo sin ningún movimiento
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NewgestDbContext>();

        var grupo = await db.GruposArticulos
            .FirstAsync(g => g.Descripcion == "STK-GRP-TEST");

        var articuloSinStock = NewGest.Domain.Entities.Neg.Articulo.Crear(
            NewgestWebApplicationFactory.IdEmpresaTest,
            "STK-T-SIN",
            "Artículo Sin Stock",
            grupo.IdGrupo,
            idUnidad: 1,
            precioLista: 100m,
            precioCosto: 50m,
            porcentajeIva: 21m,
            observaciones: null);
        db.Articulos.Add(articuloSinStock);
        await db.SaveChangesAsync();

        using var client = ClienteConToken();
        var response = await client.GetAsync($"/api/stock/{articuloSinStock.IdArticulo}/existencias");

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "el endpoint debe retornar 200 incluso si no hay existencias");
        var existencias = await response.Content.ReadFromJsonAsync<List<ExistenciaDepositoDto>>(JsonOpts);
        existencias.Should().NotBeNull();
        existencias!.Should().BeEmpty("un artículo sin movimientos no tiene existencias");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GET /api/stock/{idArticulo}/movimientos
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GET_historial_movimientos_con_jwt_retorna_200_y_historial_paginado()
    {
        using var client = ClienteConToken();

        // Registrar dos movimientos para tener historial
        var dto1 = new RegistrarMovimientoDto(_idArticuloTest, _idDepositoTest,
            TipoMovimiento.Entrada, 5m, 100m, null, null, "mov1");
        var dto2 = new RegistrarMovimientoDto(_idArticuloTest, _idDepositoTest,
            TipoMovimiento.Entrada, 3m, 100m, null, null, "mov2");
        await client.PostAsJsonAsync("/api/stock/movimientos", dto1);
        await client.PostAsJsonAsync("/api/stock/movimientos", dto2);

        var response = await client.GetAsync($"/api/stock/{_idArticuloTest}/movimientos?page=1&pageSize=20");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty("la respuesta no debe estar vacía");

        // Verificar que el resultado tiene estructura paginada o lista
        using var doc = JsonDocument.Parse(content);
        // El endpoint puede retornar PagedResult o lista directa
        doc.RootElement.ValueKind.Should().BeOneOf(
            new[] { JsonValueKind.Object, JsonValueKind.Array },
            "la respuesta debe ser un objeto paginado o un array");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GET /api/stock/alertas-reposicion
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GET_alertas_reposicion_con_jwt_retorna_200()
    {
        using var client = ClienteConToken();

        var response = await client.GetAsync("/api/stock/alertas-reposicion");

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "el endpoint de alertas debe retornar 200 aunque la lista esté vacía");

        var lista = await response.Content.ReadFromJsonAsync<List<ExistenciaDepositoDto>>(JsonOpts);
        lista.Should().NotBeNull("la respuesta debe ser una lista (puede estar vacía)");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // CONCURRENCIA — criterio de aceptación crítico
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task POST_movimientos_concurrentes_no_generan_inconsistencias()
    {
        // Registrar 10 entradas de 1 unidad concurrentes sobre el mismo artículo+depósito.
        // Equivale al test del RLOCK() de VFP: la existencia final debe ser exactamente 10.
        // (El test asume que la existencia parte de 0 para este artículo en este depósito.)

        // Primero resetear la existencia a 0 con un ajuste si hubiera saldo previo
        using var setupClient = ClienteConToken();
        using var scopeInit = _factory.Services.CreateScope();
        var dbInit = scopeInit.ServiceProvider.GetRequiredService<NewgestDbContext>();
        var existenciaPrevia = await dbInit.ExistenciasDeposito
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.IdArticulo == _idArticuloTest && e.IdDeposito == _idDepositoTest);

        if (existenciaPrevia != null && existenciaPrevia.Cantidad != 0)
        {
            // No hay ajuste a cero porque cantidad debe ser > 0 según DomainException.
            // Limpiar directo en BD para garantizar punto de partida conocido.
            await dbInit.Database.ExecuteSqlRawAsync(
                "UPDATE inv.ExistenciasDeposito SET Cantidad = 0 WHERE IdArticulo = {0} AND IdDeposito = {1}",
                _idArticuloTest, _idDepositoTest);
        }

        const int cantidad = 10;
        using var concurrentClient = ClienteConToken();
        var tasks = Enumerable.Range(0, cantidad).Select(_ =>
            concurrentClient.PostAsJsonAsync("/api/stock/movimientos", new RegistrarMovimientoDto(
                _idArticuloTest,
                _idDepositoTest,
                TipoMovimiento.Entrada,
                1m,
                100m,
                null,
                null,
                "movimiento concurrente"))
        ).ToList();

        var responses = await Task.WhenAll(tasks);

        responses.Should().OnlyContain(r => r.IsSuccessStatusCode,
            "todas las entradas concurrentes deben ser aceptadas");

        // Verificar que la existencia final es exactamente 10 (no duplicados, no pérdidas)
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NewgestDbContext>();
        var existenciaFinal = await db.ExistenciasDeposito
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.IdArticulo == _idArticuloTest && e.IdDeposito == _idDepositoTest);

        existenciaFinal.Should().NotBeNull();
        existenciaFinal!.Cantidad.Should().Be(cantidad,
            "10 entradas de 1 unidad concurrentes deben resultar en exactamente 10 unidades de existencia");

        // Verificar que se registraron exactamente 10 movimientos (sin duplicados ni pérdidas)
        var cantidadMovimientos = await db.MovimientosStock
            .CountAsync(m => m.IdArticulo == _idArticuloTest
                           && m.IdDeposito == _idDepositoTest
                           && m.Observaciones == "movimiento concurrente");

        cantidadMovimientos.Should().Be(cantidad,
            "deben existir exactamente 10 registros de movimiento en BD");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Helpers privados
    // ──────────────────────────────────────────────────────────────────────────

    private async Task<string> LoginYObtenerTokenAsync()
    {
        var dto = new NewGest.Application.DTOs.LoginDto(
            NewgestWebApplicationFactory.IdEmpresaTest,
            NewgestWebApplicationFactory.UsuarioAdmin,
            NewgestWebApplicationFactory.PasswordAdmin);

        var response = await _client.PostAsJsonAsync("/api/auth/login", dto);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<NewGest.Application.DTOs.LoginResultDto>(JsonOpts);
        return result!.Token;
    }

    private HttpClient ClienteConToken()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _token);
        return client;
    }
}

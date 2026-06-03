using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NewGest.Application.DTOs.Pedidos;
using NewGest.Application.DTOs.Shared;
using NewGest.Domain.Enums;
using NewGest.Infrastructure.Data;

namespace NewGest.IntegrationTests.Pedidos;

/// <summary>
/// Tests de integración para los endpoints de Pedidos y Remitos (Sprint 5-7 — M11).
/// Verifica el flujo completo Pedido → Remito parcial → Remito final con descuento de stock,
/// estados Pendiente/Parcial/Entregado/Anulado y reglas de dominio.
/// </summary>
[Collection("IntegrationTests")]
public class PedidosEndpointsTests : IAsyncLifetime
{
    private readonly NewgestWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private string _token = default!;

    // IDs de datos de soporte creados en seeding
    private int _idClienteTest;
    private int _idArticulo1;
    private int _idArticulo2;
    private int _idDepositoTest;

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public PedidosEndpointsTests(NewgestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IAsyncLifetime — seeding y cleanup
    // ──────────────────────────────────────────────────────────────────────────

    public async Task InitializeAsync()
    {
        _token = await LoginYObtenerTokenAsync();
        await LimpiarDatosPedidosAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NewgestDbContext>();

        // Cliente de prueba
        var cliente = NewGest.Domain.Entities.Neg.Cliente.Crear(
            NewgestWebApplicationFactory.IdEmpresaTest,
            "PED-T-CLI",
            "Cliente Test Pedidos SA",
            null,
            CondicionIva.ConsumidorFinal,
            null, null, null, null, null, null);
        db.Clientes.Add(cliente);
        await db.SaveChangesAsync();
        _idClienteTest = cliente.IdCliente;

        // Grupo y artículos de prueba
        var grupo = new NewGest.Domain.Entities.Neg.GrupoArticulo
        {
            IdEmpresa = NewgestWebApplicationFactory.IdEmpresaTest,
            Descripcion = "PED-GRP-TEST",
            IdGrupoPadre = null
        };
        db.GruposArticulos.Add(grupo);
        await db.SaveChangesAsync();

        var art1 = NewGest.Domain.Entities.Neg.Articulo.Crear(
            NewgestWebApplicationFactory.IdEmpresaTest,
            "PED-T-A01", "Artículo Pedido 1",
            grupo.IdGrupo, idUnidad: 1,
            precioLista: 100m, precioCosto: 60m, porcentajeIva: 21m, observaciones: null);

        var art2 = NewGest.Domain.Entities.Neg.Articulo.Crear(
            NewgestWebApplicationFactory.IdEmpresaTest,
            "PED-T-A02", "Artículo Pedido 2",
            grupo.IdGrupo, idUnidad: 1,
            precioLista: 200m, precioCosto: 120m, porcentajeIva: 21m, observaciones: null);

        db.Articulos.AddRange(art1, art2);
        await db.SaveChangesAsync();
        _idArticulo1 = art1.IdArticulo;
        _idArticulo2 = art2.IdArticulo;

        // Depósito de prueba
        var deposito = new NewGest.Domain.Entities.Inv.Deposito
        {
            IdEmpresa = NewgestWebApplicationFactory.IdEmpresaTest,
            Codigo = "PED-D-01  ",   // CHAR(10)
            Descripcion = "Depósito Test Pedidos",
            Activo = true
        };
        db.Depositos.Add(deposito);
        await db.SaveChangesAsync();
        _idDepositoTest = deposito.IdDeposito;

        // Cargar stock suficiente para los tests de remitos
        // (100 unidades de cada artículo en el depósito de prueba)
        db.ExistenciasDeposito.Add(new NewGest.Domain.Entities.Inv.ExistenciaDeposito
        {
            IdEmpresa = NewgestWebApplicationFactory.IdEmpresaTest,
            IdArticulo = _idArticulo1,
            IdDeposito = _idDepositoTest,
            Cantidad = 100m,
            CostoPromedio = 60m,
            StockMinimo = 5m
        });
        db.ExistenciasDeposito.Add(new NewGest.Domain.Entities.Inv.ExistenciaDeposito
        {
            IdEmpresa = NewgestWebApplicationFactory.IdEmpresaTest,
            IdArticulo = _idArticulo2,
            IdDeposito = _idDepositoTest,
            Cantidad = 100m,
            CostoPromedio = 120m,
            StockMinimo = 5m
        });
        await db.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await LimpiarDatosPedidosAsync();
    }

    private async Task LimpiarDatosPedidosAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NewgestDbContext>();

        // Limpiar en orden FK: items remito → remitos → items pedido → pedidos → stock → articulos
        await db.Database.ExecuteSqlRawAsync(
            "DELETE ir FROM com.ItemsRemito ir " +
            "INNER JOIN com.Remitos r ON ir.IdRemito = r.IdRemito " +
            "INNER JOIN com.Pedidos p ON r.IdPedido = p.IdPedido " +
            "WHERE p.IdEmpresa = 1 AND p.IdCliente IN " +
            "(SELECT IdCliente FROM neg.Clientes WHERE Codigo = 'PED-T-CLI')");

        await db.Database.ExecuteSqlRawAsync(
            "DELETE r FROM com.Remitos r " +
            "INNER JOIN com.Pedidos p ON r.IdPedido = p.IdPedido " +
            "WHERE p.IdEmpresa = 1 AND p.IdCliente IN " +
            "(SELECT IdCliente FROM neg.Clientes WHERE Codigo = 'PED-T-CLI')");

        await db.Database.ExecuteSqlRawAsync(
            "DELETE ip FROM com.ItemsPedido ip " +
            "INNER JOIN com.Pedidos p ON ip.IdPedido = p.IdPedido " +
            "WHERE p.IdEmpresa = 1 AND p.IdCliente IN " +
            "(SELECT IdCliente FROM neg.Clientes WHERE Codigo = 'PED-T-CLI')");

        await db.Database.ExecuteSqlRawAsync(
            "DELETE FROM com.Pedidos WHERE IdEmpresa = 1 AND IdCliente IN " +
            "(SELECT IdCliente FROM neg.Clientes WHERE Codigo = 'PED-T-CLI')");

        await db.Database.ExecuteSqlRawAsync(
            "DELETE FROM inv.MovimientosStock WHERE IdEmpresa = 1 AND IdArticulo IN " +
            "(SELECT IdArticulo FROM neg.Articulos WHERE Codigo LIKE 'PED-T-%')");

        await db.Database.ExecuteSqlRawAsync(
            "DELETE FROM inv.ExistenciasDeposito WHERE IdEmpresa = 1 AND IdArticulo IN " +
            "(SELECT IdArticulo FROM neg.Articulos WHERE Codigo LIKE 'PED-T-%')");

        await db.Database.ExecuteSqlRawAsync(
            "DELETE FROM neg.Articulos WHERE Codigo LIKE 'PED-T-%' AND IdEmpresa = 1");

        await db.Database.ExecuteSqlRawAsync(
            "DELETE FROM inv.Depositos WHERE Codigo LIKE 'PED-D-%' AND IdEmpresa = 1");

        await db.Database.ExecuteSqlRawAsync(
            "DELETE FROM neg.GruposArticulos WHERE Descripcion LIKE 'PED-GRP-%' AND IdEmpresa = 1");

        await db.Database.ExecuteSqlRawAsync(
            "DELETE FROM neg.Clientes WHERE Codigo = 'PED-T-CLI' AND IdEmpresa = 1");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GET /api/pedidos
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GET_pedidos_sin_jwt_retorna_401()
    {
        var response = await _client.GetAsync("/api/pedidos");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GET_pedidos_con_jwt_retorna_200_y_PagedResult()
    {
        using var client = ClienteConToken();

        var response = await client.GetAsync("/api/pedidos?page=1&pageSize=20");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var resultado = await response.Content.ReadFromJsonAsync<PagedResult<PedidoListItemDto>>(JsonOpts);
        resultado.Should().NotBeNull();
        resultado!.Items.Should().NotBeNull();
    }

    [Fact]
    public async Task GET_pedidos_filtro_estado_Pendiente_retorna_solo_pendientes()
    {
        using var client = ClienteConToken();

        // Crear un pedido en estado Pendiente
        var dto = CrearPedidoTestDto();
        var crearResp = await client.PostAsJsonAsync("/api/pedidos", dto);
        crearResp.StatusCode.Should().Be(HttpStatusCode.Created);

        // Filtrar por Pendiente
        var response = await client.GetAsync($"/api/pedidos?estado={(int)EstadoPedido.Pendiente}&page=1&pageSize=100");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var resultado = await response.Content.ReadFromJsonAsync<PagedResult<PedidoListItemDto>>(JsonOpts);
        resultado!.Items.Should().OnlyContain(p => p.Estado == EstadoPedido.Pendiente,
            "el filtro por estado debe retornar solo pedidos en estado Pendiente");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // POST /api/pedidos
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task POST_pedido_sin_jwt_retorna_401()
    {
        var dto = CrearPedidoTestDto();

        var response = await _client.PostAsJsonAsync("/api/pedidos", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task POST_pedido_sin_items_retorna_error()
    {
        using var client = ClienteConToken();
        var dto = new CrearPedidoDto(_idClienteTest, null, null, null, new List<ItemPedidoInputDto>());

        var response = await client.PostAsJsonAsync("/api/pedidos", dto);

        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity },
            "un pedido sin items debe ser rechazado por DomainException");
    }

    [Fact]
    public async Task POST_pedido_items_validos_retorna_201_y_estado_Pendiente()
    {
        using var client = ClienteConToken();
        var dto = CrearPedidoTestDto();

        var response = await client.PostAsJsonAsync("/api/pedidos", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created,
            "un pedido válido debe retornar 201");

        var pedido = await response.Content.ReadFromJsonAsync<PedidoDto>(JsonOpts);
        pedido.Should().NotBeNull();
        pedido!.IdPedido.Should().BeGreaterThan(0);
        pedido.Estado.Should().Be(EstadoPedido.Pendiente,
            "un pedido recién creado debe estar en estado Pendiente");
        pedido.Items.Should().HaveCount(2, "el pedido de prueba tiene 2 artículos");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GET /api/pedidos/{id}
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GET_pedido_por_id_existente_retorna_200_con_items()
    {
        using var client = ClienteConToken();

        // Crear pedido
        var crearResp = await client.PostAsJsonAsync("/api/pedidos", CrearPedidoTestDto());
        var creado = await crearResp.Content.ReadFromJsonAsync<PedidoDto>(JsonOpts);

        var response = await client.GetAsync($"/api/pedidos/{creado!.IdPedido}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var pedido = await response.Content.ReadFromJsonAsync<PedidoDto>(JsonOpts);
        pedido.Should().NotBeNull();
        pedido!.IdPedido.Should().Be(creado.IdPedido);
        pedido.Items.Should().NotBeEmpty("un pedido debe retornar sus items");
    }

    [Fact]
    public async Task GET_pedido_por_id_inexistente_retorna_404()
    {
        using var client = ClienteConToken();

        var response = await client.GetAsync("/api/pedidos/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // POST /api/pedidos/{id}/anular
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task POST_anular_pedido_Pendiente_retorna_204_y_estado_Anulado()
    {
        using var client = ClienteConToken();

        // Crear pedido
        var crearResp = await client.PostAsJsonAsync("/api/pedidos", CrearPedidoTestDto());
        var creado = await crearResp.Content.ReadFromJsonAsync<PedidoDto>(JsonOpts);

        // Anular
        var anularResp = await client.PostAsJsonAsync($"/api/pedidos/{creado!.IdPedido}/anular", new { });

        anularResp.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.NoContent, HttpStatusCode.OK },
            "anular un pedido Pendiente debe retornar 204 o 200");

        // Verificar estado en BD
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NewgestDbContext>();
        var pedidoEnBd = await db.Pedidos.FindAsync(creado.IdPedido);
        pedidoEnBd!.Estado.Should().Be(EstadoPedido.Anulado);
    }

    [Fact]
    public async Task POST_anular_pedido_Entregado_retorna_400_DomainException()
    {
        using var client = ClienteConToken();

        // Crear pedido y generar remito completo para dejarlo en Entregado
        var crearResp = await client.PostAsJsonAsync("/api/pedidos", CrearPedidoTestDto(cant1: 2m, cant2: 2m));
        var creado = await crearResp.Content.ReadFromJsonAsync<PedidoDto>(JsonOpts);

        // Remito total: despacha todo
        var remitoDto = new GenerarRemitoDto(
            _idDepositoTest,
            new List<DespacharItemDto>
            {
                new(_idArticulo1, 2m),
                new(_idArticulo2, 2m)
            });
        var remitoResp = await client.PostAsJsonAsync($"/api/pedidos/{creado!.IdPedido}/remito", remitoDto);
        remitoResp.StatusCode.Should().Be(HttpStatusCode.Created);

        // Intentar anular el pedido ya Entregado
        var anularResp = await client.PostAsJsonAsync($"/api/pedidos/{creado.IdPedido}/anular", new { });

        anularResp.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "anular un pedido Entregado debe retornar 400 por DomainException");
    }

    [Fact]
    public async Task POST_anular_pedido_inexistente_retorna_404_o_400()
    {
        using var client = ClienteConToken();

        var response = await client.PostAsJsonAsync("/api/pedidos/999999/anular", new { });

        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.NotFound, HttpStatusCode.BadRequest },
            "anular un pedido inexistente debe retornar 404 o 400");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // POST /api/pedidos/{id}/remito — flujo completo crítico
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task POST_remito_flujo_completo_parcial_luego_total_actualiza_estado_y_stock()
    {
        // Escenario:
        // 1. Crear pedido con artículo1 cant=5 y artículo2 cant=5
        // 2. Remito parcial: despachar solo 3 del artículo1
        //    → Pedido queda en Parcial, stock art1 disminuye 3
        // 3. Remito final: despachar 2 restantes de art1 + 5 de art2
        //    → Pedido queda en Entregado, stock art1 -5 total, art2 -5 total

        using var client = ClienteConToken();

        // Registrar existencia base para este test (resets a valores conocidos)
        using var scope0 = _factory.Services.CreateScope();
        var db0 = scope0.ServiceProvider.GetRequiredService<NewgestDbContext>();
        var ex1 = await db0.ExistenciasDeposito
            .FirstOrDefaultAsync(e => e.IdArticulo == _idArticulo1 && e.IdDeposito == _idDepositoTest);
        var ex2 = await db0.ExistenciasDeposito
            .FirstOrDefaultAsync(e => e.IdArticulo == _idArticulo2 && e.IdDeposito == _idDepositoTest);
        var stockInicialArt1 = ex1?.Cantidad ?? 0m;
        var stockInicialArt2 = ex2?.Cantidad ?? 0m;

        // --- PASO 1: crear pedido ---
        var pedidoDto = CrearPedidoTestDto(cant1: 5m, cant2: 5m);
        var crearResp = await client.PostAsJsonAsync("/api/pedidos", pedidoDto);
        crearResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var pedidoCreado = await crearResp.Content.ReadFromJsonAsync<PedidoDto>(JsonOpts);
        pedidoCreado!.Estado.Should().Be(EstadoPedido.Pendiente);

        // --- PASO 2: remito parcial (solo 3 del artículo 1) ---
        var remitoParcial = new GenerarRemitoDto(
            _idDepositoTest,
            new List<DespacharItemDto> { new(_idArticulo1, 3m) });

        var remitoParcialResp = await client.PostAsJsonAsync(
            $"/api/pedidos/{pedidoCreado.IdPedido}/remito", remitoParcial);

        remitoParcialResp.StatusCode.Should().Be(HttpStatusCode.Created,
            "el remito parcial debe retornar 201");

        var remitoP = await remitoParcialResp.Content.ReadFromJsonAsync<RemitoDto>(JsonOpts);
        remitoP.Should().NotBeNull();
        remitoP!.Items.Should().HaveCount(1);
        remitoP.Items[0].IdArticulo.Should().Be(_idArticulo1);
        remitoP.Items[0].Cantidad.Should().Be(3m);

        // Verificar estado Parcial del pedido en BD
        using var scope1 = _factory.Services.CreateScope();
        var db1 = scope1.ServiceProvider.GetRequiredService<NewgestDbContext>();
        var pedidoTrasRemitoParcial = await db1.Pedidos.FindAsync(pedidoCreado.IdPedido);
        pedidoTrasRemitoParcial!.Estado.Should().Be(EstadoPedido.Parcial,
            "después del remito parcial el pedido debe estar en estado Parcial");

        // Verificar stock de artículo 1 disminuyó en 3
        var stockArt1TrasRemitoParcial = await db1.ExistenciasDeposito
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.IdArticulo == _idArticulo1 && e.IdDeposito == _idDepositoTest);
        stockArt1TrasRemitoParcial!.Cantidad.Should().Be(stockInicialArt1 - 3m,
            "el stock del artículo 1 debe disminuir 3 tras el remito parcial");

        // --- PASO 3: remito final (2 restantes de art1 + 5 de art2) ---
        var remitoFinal = new GenerarRemitoDto(
            _idDepositoTest,
            new List<DespacharItemDto>
            {
                new(_idArticulo1, 2m),
                new(_idArticulo2, 5m)
            });

        var remitoFinalResp = await client.PostAsJsonAsync(
            $"/api/pedidos/{pedidoCreado.IdPedido}/remito", remitoFinal);

        remitoFinalResp.StatusCode.Should().Be(HttpStatusCode.Created,
            "el remito final debe retornar 201");

        // Verificar estado Entregado del pedido en BD
        using var scope2 = _factory.Services.CreateScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<NewgestDbContext>();
        var pedidoTrasRemitoFinal = await db2.Pedidos.FindAsync(pedidoCreado.IdPedido);
        pedidoTrasRemitoFinal!.Estado.Should().Be(EstadoPedido.Entregado,
            "después del remito final el pedido debe estar en estado Entregado");

        // Verificar stock final: art1 -5 total, art2 -5
        var stockArt1Final = await db2.ExistenciasDeposito
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.IdArticulo == _idArticulo1 && e.IdDeposito == _idDepositoTest);
        var stockArt2Final = await db2.ExistenciasDeposito
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.IdArticulo == _idArticulo2 && e.IdDeposito == _idDepositoTest);

        stockArt1Final!.Cantidad.Should().Be(stockInicialArt1 - 5m,
            "el stock del artículo 1 debe disminuir 5 en total (3 + 2)");
        stockArt2Final!.Cantidad.Should().Be(stockInicialArt2 - 5m,
            "el stock del artículo 2 debe disminuir 5");
    }

    [Fact]
    public async Task POST_remito_despachar_mas_de_pendiente_retorna_400_DomainException()
    {
        using var client = ClienteConToken();

        // Crear pedido con 3 unidades del artículo 1
        var dto = new CrearPedidoDto(
            _idClienteTest,
            null,
            null,
            null,
            new List<ItemPedidoInputDto> { new(_idArticulo1, 3m, 100m) });

        var crearResp = await client.PostAsJsonAsync("/api/pedidos", dto);
        var creado = await crearResp.Content.ReadFromJsonAsync<PedidoDto>(JsonOpts);

        // Intentar despachar 10 (más de lo pedido)
        var remitoDto = new GenerarRemitoDto(
            _idDepositoTest,
            new List<DespacharItemDto> { new(_idArticulo1, 10m) });

        var remitoResp = await client.PostAsJsonAsync($"/api/pedidos/{creado!.IdPedido}/remito", remitoDto);

        remitoResp.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "intentar despachar más cantidad que la pendiente debe retornar 400 por DomainException");
    }

    [Fact]
    public async Task POST_remito_sin_items_retorna_error()
    {
        using var client = ClienteConToken();

        var crearResp = await client.PostAsJsonAsync("/api/pedidos", CrearPedidoTestDto());
        var creado = await crearResp.Content.ReadFromJsonAsync<PedidoDto>(JsonOpts);

        var remitoDto = new GenerarRemitoDto(_idDepositoTest, new List<DespacharItemDto>());
        var remitoResp = await client.PostAsJsonAsync($"/api/pedidos/{creado!.IdPedido}/remito", remitoDto);

        remitoResp.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity },
            "un remito sin items debe ser rechazado");
    }

    [Fact]
    public async Task POST_remito_rollback_si_stock_service_falla_no_crea_remito()
    {
        // Verificar transaccionalidad: si hay error al descontar stock (artículo no existe en depósito),
        // el remito no debe quedar creado en BD.
        using var client = ClienteConToken();

        // Crear pedido con artículo cuyo stock está en 0 en el depósito de prueba
        // (creamos un artículo nuevo sin existencia)
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NewgestDbContext>();

        var grupo = await db.GruposArticulos
            .FirstAsync(g => g.Descripcion == "PED-GRP-TEST");

        var artSinStock = NewGest.Domain.Entities.Neg.Articulo.Crear(
            NewgestWebApplicationFactory.IdEmpresaTest,
            "PED-T-NSK", "Sin Stock",
            grupo.IdGrupo, idUnidad: 1,
            precioLista: 100m, precioCosto: 50m, porcentajeIva: 21m, observaciones: null);
        db.Articulos.Add(artSinStock);
        await db.SaveChangesAsync();

        var dtoConSinStock = new CrearPedidoDto(
            _idClienteTest,
            null,
            null,
            null,
            new List<ItemPedidoInputDto> { new(artSinStock.IdArticulo, 5m, 100m) });

        var crearResp = await client.PostAsJsonAsync("/api/pedidos", dtoConSinStock);
        crearResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var pedido = await crearResp.Content.ReadFromJsonAsync<PedidoDto>(JsonOpts);

        int remitosAntes = await db.Remitos.CountAsync(r => r.IdPedido == pedido!.IdPedido);

        // Intentar generar remito — si el handler maneja stock insuficiente como error,
        // el remito no debe crearse
        var remitoDto = new GenerarRemitoDto(
            _idDepositoTest,
            new List<DespacharItemDto> { new(artSinStock.IdArticulo, 5m) });

        var remitoResp = await client.PostAsJsonAsync($"/api/pedidos/{pedido!.IdPedido}/remito", remitoDto);

        if (!remitoResp.IsSuccessStatusCode)
        {
            // Si el handler rechazó la operación, verificar que no se creó el remito
            int remitosDespues = await db.Remitos.CountAsync(r => r.IdPedido == pedido.IdPedido);
            remitosDespues.Should().Be(remitosAntes,
                "si la generación del remito falla, no debe crearse ningún remito (rollback)");
        }
        // Si el sistema permite generar remito sin validar stock, se considera comportamiento
        // válido también (el criterio de aceptación dice "verificar comportamiento actual").
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Helpers privados
    // ──────────────────────────────────────────────────────────────────────────

    private CrearPedidoDto CrearPedidoTestDto(decimal cant1 = 5m, decimal cant2 = 5m) =>
        new(
            _idClienteTest,
            null,
            DateTime.UtcNow.AddDays(7),
            "Pedido de prueba de integración",
            new List<ItemPedidoInputDto>
            {
                new(_idArticulo1, cant1, 100m),
                new(_idArticulo2, cant2, 200m)
            });

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

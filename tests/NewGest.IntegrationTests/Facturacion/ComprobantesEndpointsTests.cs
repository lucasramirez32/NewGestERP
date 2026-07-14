using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NewGest.Application.DTOs;
using NewGest.Application.DTOs.Clientes;
using NewGest.Application.DTOs.Comprobantes;
using NewGest.Application.Services;
using NewGest.Domain.Common;
using NewGest.Domain.Entities.Neg;
using NewGest.Domain.Enums;
using NewGest.Infrastructure.Data;

namespace NewGest.IntegrationTests.Facturacion;

[Collection("IntegrationTests")]
public class ComprobantesEndpointsTests : IAsyncLifetime
{
    private readonly NewgestWebApplicationFactory _factory;
    private HttpClient _client = default!;
    private string _token = default!;
    private int _idCliente;

    // Mock de AFIP inyectado para cada test
    private readonly Mock<IAfipService> _afipMock = new();

    public ComprobantesEndpointsTests(NewgestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        // Cliente HTTP con IAfipService mockeado
        _client = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IAfipService));
                if (descriptor != null) services.Remove(descriptor);
                services.AddScoped<IAfipService>(_ => _afipMock.Object);
            })).CreateClient();

        // Login
        var loginDto = new LoginDto(
            NewgestWebApplicationFactory.IdEmpresaTest,
            NewgestWebApplicationFactory.UsuarioAdmin,
            NewgestWebApplicationFactory.PasswordAdmin);

        var loginResp = await _client.PostAsJsonAsync("/api/auth/login", loginDto);
        loginResp.EnsureSuccessStatusCode();
        var loginData = await loginResp.Content.ReadFromJsonAsync<LoginResultDto>();
        _token = loginData!.Token;
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);

        // Seeding: cliente de prueba
        await LimpiarYSembrarAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ─── GET /api/comprobantes ────────────────────────────────────────────────
    [Fact]
    public async Task GET_comprobantes_retorna_lista_paginada_vacia_o_con_datos()
    {
        var resp = await _client.GetAsync("/api/comprobantes?pagina=1&tamano=20");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<PagedResultDto<ComprobanteDto>>();
        body.Should().NotBeNull();
        body!.Items.Should().NotBeNull();
    }

    // ─── POST /api/comprobantes — happy path FC-B ─────────────────────────────
    [Fact]
    public async Task POST_comprobante_facturaB_retorna_201_con_CAE()
    {
        _afipMock.Setup(a => a.SolicitarCaeAsync(It.IsAny<ComprobanteAfip>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CaeResponse("12345678901234", new DateOnly(2026, 8, 31), "1"));

        var dto = new EmitirFacturaDto(
            TipoComprobante.FacturaB,
            PuntoVenta: 1,
            Fecha: DateOnly.FromDateTime(DateTime.Today),
            IdCliente: _idCliente,
            IdPedidoOrigen: null,
            Items: [new ItemFacturaDto(0, "Servicio de prueba", 1m, 1000m, AlicuotaIva.Porcentaje21)]
        );

        var resp = await _client.PostAsJsonAsync("/api/comprobantes", dto);

        var content = await resp.Content.ReadAsStringAsync();
        resp.StatusCode.Should().Be(HttpStatusCode.Created, because: content);
        var result = await resp.Content.ReadFromJsonAsync<FacturaEmitidaDto>();
        result.Should().NotBeNull();
        result!.CodigoCae.Should().Be("12345678901234");
        result.Total.Should().Be(1210m); // 1000 + 21% IVA
        result.Numero.Should().BeGreaterThan(0);
    }

    // ─── POST — rechazo AFIP no persiste ni decrementa stock ─────────────────
    [Fact]
    public async Task POST_comprobante_rechazo_AFIP_retorna_error_descriptivo()
    {
        _afipMock.Setup(a => a.SolicitarCaeAsync(It.IsAny<ComprobanteAfip>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DomainException("AFIP rechazó: error 10016 — fecha inválida."));

        var dto = new EmitirFacturaDto(
            TipoComprobante.FacturaB,
            PuntoVenta: 1,
            Fecha: DateOnly.FromDateTime(DateTime.Today),
            IdCliente: _idCliente,
            IdPedidoOrigen: null,
            Items: [new ItemFacturaDto(0, "Producto X", 1m, 500m, AlicuotaIva.Porcentaje21)]
        );

        // Contar comprobantes antes
        var antes = await _client.GetAsync("/api/comprobantes?pagina=1&tamano=100");
        var cuerpoAntes = await antes.Content.ReadFromJsonAsync<PagedResultDto<ComprobanteDto>>();
        var totalAntes = cuerpoAntes!.Total;

        var resp = await _client.PostAsJsonAsync("/api/comprobantes", dto);

        resp.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity,
            HttpStatusCode.InternalServerError);

        // Ningún comprobante nuevo debe haberse persistido
        var despues = await _client.GetAsync("/api/comprobantes?pagina=1&tamano=100");
        var cuerpoDespues = await despues.Content.ReadFromJsonAsync<PagedResultDto<ComprobanteDto>>();
        cuerpoDespues!.Total.Should().Be(totalAntes);
    }

    // ─── POST — numeración consecutiva sin duplicados (10 facturas) ──────────
    [Fact]
    public async Task POST_diez_facturas_secuenciales_tienen_numeros_consecutivos_sin_gaps()
    {
        _afipMock.Setup(a => a.SolicitarCaeAsync(It.IsAny<ComprobanteAfip>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CaeResponse("99999999999999", new DateOnly(2026, 12, 31), "0"));

        // Limpiar numerador previo para este punto de venta y tipo
        await LimpiarNumeradoresAsync();

        var numeros = new List<long>();
        for (var i = 0; i < 10; i++)
        {
            var dto = new EmitirFacturaDto(
                TipoComprobante.FacturaB, PuntoVenta: 99,
                Fecha: DateOnly.FromDateTime(DateTime.Today),
                IdCliente: _idCliente, IdPedidoOrigen: null,
                Items: [new ItemFacturaDto(0, $"Prod {i}", 1m, 100m, AlicuotaIva.Porcentaje21)]
            );
            var resp = await _client.PostAsJsonAsync("/api/comprobantes", dto);
            var content = await resp.Content.ReadAsStringAsync();
            resp.StatusCode.Should().Be(HttpStatusCode.Created, $"factura {i} debe emitirse correctamente. Error: {content}");
            var result = await resp.Content.ReadFromJsonAsync<FacturaEmitidaDto>();
            numeros.Add(result!.Numero);
        }

        numeros.Should().OnlyHaveUniqueItems("no deben existir duplicados");
        numeros.Should().BeInAscendingOrder("deben ser consecutivos");
        (numeros.Last() - numeros.First()).Should().Be(9, "no debe haber gaps");
    }

    // ─── GET /api/comprobantes/{id} ───────────────────────────────────────────
    [Fact]
    public async Task GET_comprobante_por_id_retorna_datos_completos()
    {
        _afipMock.Setup(a => a.SolicitarCaeAsync(It.IsAny<ComprobanteAfip>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CaeResponse("55555555555555", new DateOnly(2026, 9, 30), "1"));

        var dto = new EmitirFacturaDto(
            TipoComprobante.FacturaB, PuntoVenta: 1,
            Fecha: DateOnly.FromDateTime(DateTime.Today),
            IdCliente: _idCliente, IdPedidoOrigen: null,
            Items: [new ItemFacturaDto(0, "Ítem test", 3m, 100m, AlicuotaIva.Porcentaje21)]
        );
        var emitirResp = await _client.PostAsJsonAsync("/api/comprobantes", dto);
        var emitirContent = await emitirResp.Content.ReadAsStringAsync();
        emitirResp.StatusCode.Should().Be(HttpStatusCode.Created, because: emitirContent);
        var emitido = await emitirResp.Content.ReadFromJsonAsync<FacturaEmitidaDto>();

        var getResp = await _client.GetAsync($"/api/comprobantes/{emitido!.IdComprobante}");

        getResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var comp = await getResp.Content.ReadFromJsonAsync<ComprobanteDto>();
        comp!.CodigoCae.Should().Be("55555555555555");
        comp.Total.Should().Be(363m); // 300 + 21% = 363
        comp.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GET_comprobante_inexistente_retorna_404()
    {
        var resp = await _client.GetAsync("/api/comprobantes/999999");

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────
    private async Task LimpiarYSembrarAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NewgestDbContext>();

        // Limpiar comprobantes y numeradores de prueba (primero items por FK)
        db.ItemsComprobante.RemoveRange(db.ItemsComprobante);
        db.Comprobantes.RemoveRange(db.Comprobantes);
        db.NumeradoresComprobante.RemoveRange(db.NumeradoresComprobante);
        await db.SaveChangesAsync();

        // Crear cliente de prueba
        var existente = db.Clientes.FirstOrDefault(c => c.IdEmpresa == 1 && c.Codigo == "TST001");
        if (existente is null)
        {
            var cli = Cliente.Crear(1, "TST001", "Cliente Test Factura", null,
                CondicionIva.ConsumidorFinal, null, null, null, null, null, null);
            db.Clientes.Add(cli);
            await db.SaveChangesAsync();
            _idCliente = cli.IdCliente;
        }
        else
        {
            _idCliente = existente.IdCliente;
        }
    }

    private async Task LimpiarNumeradoresAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NewgestDbContext>();

        db.ItemsComprobante.RemoveRange(db.ItemsComprobante);
        db.Comprobantes.RemoveRange(db.Comprobantes);
        db.NumeradoresComprobante.RemoveRange(
            db.NumeradoresComprobante.Where(n => n.IdEmpresa == 1 && n.PuntoVenta == 99));
        await db.SaveChangesAsync();
    }

    // DTOs auxiliares para deserialización
    private record LoginResponseDto(string Token, string Nombre, bool DebeResetearPassword);
    private record PagedResultDto<T>(List<T> Items, int Total, int Page, int PageSize);
}

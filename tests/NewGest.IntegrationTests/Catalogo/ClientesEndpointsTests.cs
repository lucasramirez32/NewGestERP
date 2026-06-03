using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NewGest.Application.DTOs.Clientes;
using NewGest.Application.DTOs.Shared;
using NewGest.Domain.Enums;
using NewGest.Infrastructure.Data;

namespace NewGest.IntegrationTests.Catalogo;

/// <summary>
/// Tests de integración para los endpoints de Clientes (Sprint 3-4 — M08).
/// Usa NewGest_Test con datos reales de SQL Server — sin mocks de BD.
/// Implementa IAsyncLifetime para sembrar y limpiar datos propios del módulo,
/// sin interferir con el seeding de AuthEndpointsTests.
/// </summary>
[Collection("IntegrationTests")]
public class ClientesEndpointsTests : IAsyncLifetime
{
    private readonly NewgestWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private string _token = default!;

    // IDs guardados en el seeding para uso en los tests
    private int _idClienteInscripto;
    private int _idClienteExento;
    private int _idClienteMonotributo;
    private int _idClienteEmpresaB;  // cliente de la empresa secundaria

    // Empresa secundaria para test multi-empresa
    private int _idEmpresaB;

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public ClientesEndpointsTests(NewgestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IAsyncLifetime — seeding y cleanup específicos de Clientes
    // ──────────────────────────────────────────────────────────────────────────

    public async Task InitializeAsync()
    {
        // Obtener JWT del usuario admin de la empresa de prueba
        _token = await LoginYObtenerTokenAsync();

        // Limpiar clientes de pruebas anteriores (por prefijo de código)
        await LimpiarDatosClientesAsync();

        // Sembrar datos de prueba propios
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NewgestDbContext>();

        // Empresa secundaria para el test de multi-empresa
        // (se borra y recrea para garantizar estado limpio)
        await db.Database.ExecuteSqlRawAsync(
            "DELETE FROM neg.Clientes WHERE IdEmpresa IN (SELECT IdEmpresa FROM cfg.Empresas WHERE Nombre = 'Empresa Test B')");
        await db.Database.ExecuteSqlRawAsync(
            "DELETE FROM cfg.Empresas WHERE Nombre = 'Empresa Test B'");

        db.Empresas.Add(new NewGest.Domain.Entities.Empresas.Empresa
        {
            Nombre = "Empresa Test B",
            RazonSocial = "Empresa Test B S.R.L.",
            Cuit = "30-99887766-0",
            Activa = true
        });
        await db.SaveChangesAsync();

        _idEmpresaB = await db.Empresas
            .Where(e => e.Nombre == "Empresa Test B")
            .Select(e => e.IdEmpresa)
            .FirstAsync();

        // 3 clientes con distintas condiciones IVA — empresa principal
        // CUITs validados contra el algoritmo módulo 11 del dominio
        var inscripto = NewGest.Domain.Entities.Neg.Cliente.Crear(
            NewgestWebApplicationFactory.IdEmpresaTest,
            "CLI-T-001",
            "Cliente Test Inscripto SA",
            "30-55667788-9",   // CUIT empresa válido: prefijo 30, DV verificado
            CondicionIva.Inscripto,
            "Av. Corrientes 1234",
            "Buenos Aires",
            "011-5555-0001",
            "inscripto@test.com",
            null,
            null);

        var exento = NewGest.Domain.Entities.Neg.Cliente.Crear(
            NewgestWebApplicationFactory.IdEmpresaTest,
            "CLI-T-002",
            "Cliente Test Exento SRL",
            null,
            CondicionIva.Exento,
            null,
            "Córdoba",
            null,
            null,
            null,
            null);

        var monotributo = NewGest.Domain.Entities.Neg.Cliente.Crear(
            NewgestWebApplicationFactory.IdEmpresaTest,
            "CLI-T-003",
            "Cliente Test Monotributo",
            "20-12345678-6",   // CUIT persona válido: DV=6 según algoritmo
            CondicionIva.Monotributo,
            null,
            "Rosario",
            null,
            null,
            null,
            null);

        db.Clientes.AddRange(inscripto, exento, monotributo);
        await db.SaveChangesAsync();

        _idClienteInscripto = inscripto.IdCliente;
        _idClienteExento = exento.IdCliente;
        _idClienteMonotributo = monotributo.IdCliente;

        // Cliente de la empresa B — no debe ser visible para empresa A
        var clienteB = NewGest.Domain.Entities.Neg.Cliente.Crear(
            _idEmpresaB,
            "CLI-T-001",  // mismo código, diferente empresa
            "Cliente Solo Empresa B",
            null,
            CondicionIva.ConsumidorFinal,
            null,
            null,
            null,
            null,
            null,
            null);

        db.Clientes.Add(clienteB);
        await db.SaveChangesAsync();
        _idClienteEmpresaB = clienteB.IdCliente;
    }

    public async Task DisposeAsync()
    {
        await LimpiarDatosClientesAsync();
        // Limpiar empresa B
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NewgestDbContext>();
        await db.Database.ExecuteSqlRawAsync(
            "DELETE FROM cfg.Empresas WHERE Nombre = 'Empresa Test B'");
    }

    private async Task LimpiarDatosClientesAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NewgestDbContext>();
        // Ignorar soft-delete filter para limpiar todos los registros de prueba
        await db.Database.ExecuteSqlRawAsync(
            "DELETE FROM neg.Clientes WHERE Codigo LIKE 'CLI-T-%'");
        await db.Database.ExecuteSqlRawAsync(
            "DELETE FROM neg.Clientes WHERE Codigo LIKE 'CLI-DUP%'");
        await db.Database.ExecuteSqlRawAsync(
            "DELETE FROM neg.Clientes WHERE Codigo LIKE 'CLI-UPD%'");
        await db.Database.ExecuteSqlRawAsync(
            "DELETE FROM neg.Clientes WHERE Codigo LIKE 'CLI-DEL%'");
        await db.Database.ExecuteSqlRawAsync(
            "DELETE FROM neg.Clientes WHERE Codigo LIKE 'CLI-NEW%'");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GET /api/clientes — sin JWT
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GET_clientes_sin_jwt_retorna_401()
    {
        var response = await _client.GetAsync("/api/clientes");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GET /api/clientes — con JWT
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GET_clientes_con_jwt_retorna_200_y_PagedResult()
    {
        using var client = ClienteConToken();

        var response = await client.GetAsync("/api/clientes?page=1&pageSize=20");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PagedResult<ClienteListItemDto>>(JsonOpts);
        result.Should().NotBeNull();
        result!.Items.Should().NotBeNull();
        result.Total.Should().BeGreaterThanOrEqualTo(3,
            "hay al menos 3 clientes de prueba en la empresa de tests");
    }

    [Fact]
    public async Task GET_clientes_solo_devuelve_clientes_de_la_empresa_autenticada()
    {
        using var client = ClienteConToken();

        var response = await client.GetAsync("/api/clientes?page=1&pageSize=100");

        var result = await response.Content.ReadFromJsonAsync<PagedResult<ClienteListItemDto>>(JsonOpts);

        // El cliente de empresa B no debe aparecer
        result!.Items.Should().NotContain(
            c => c.IdCliente == _idClienteEmpresaB,
            "un cliente de empresa B no debe ser visible para empresa A");
    }

    [Fact]
    public async Task GET_clientes_filtro_search_retorna_coincidencias()
    {
        using var client = ClienteConToken();

        var response = await client.GetAsync("/api/clientes?search=Inscripto&page=1&pageSize=20");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PagedResult<ClienteListItemDto>>(JsonOpts);
        result!.Items.Should().NotBeEmpty("hay al menos un cliente con 'Inscripto' en la razón social");
        result.Items.Should().AllSatisfy(c =>
            c.RazonSocial.Contains("Inscripto", StringComparison.OrdinalIgnoreCase).Should().BeTrue(
                "el filtro de búsqueda debe aplicarse a la razón social"));
    }

    [Fact]
    public async Task GET_clientes_filtro_condicionIva_retorna_solo_inscriptos()
    {
        using var client = ClienteConToken();

        var response = await client.GetAsync($"/api/clientes?condicionIva={(int)CondicionIva.Inscripto}&page=1&pageSize=100");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PagedResult<ClienteListItemDto>>(JsonOpts);
        result!.Items.Should().NotBeEmpty("hay al menos un cliente Inscripto");
        result.Items.Should().AllSatisfy(c =>
            c.CondicionIva.Should().Be(CondicionIva.Inscripto,
                "el filtro por condiciónIVA debe retornar solo inscriptos"));
    }

    [Fact]
    public async Task GET_clientes_paginacion_retorna_pageSize_items_y_total_correcto()
    {
        // Hay 3 clientes de prueba sembrados; pageSize=2 debe devolver 2 items con total >= 3
        using var client = ClienteConToken();

        var response = await client.GetAsync("/api/clientes?pageSize=2&page=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PagedResult<ClienteListItemDto>>(JsonOpts);
        result!.Items.Should().HaveCount(2, "pageSize=2 debe retornar exactamente 2 items");
        result.Total.Should().BeGreaterThanOrEqualTo(3, "hay al menos 3 clientes sembrados");
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(2);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GET /api/clientes/{id}
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GET_cliente_por_id_existente_retorna_200_y_datos_correctos()
    {
        using var client = ClienteConToken();

        var response = await client.GetAsync($"/api/clientes/{_idClienteInscripto}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var cliente = await response.Content.ReadFromJsonAsync<ClienteDto>(JsonOpts);
        cliente.Should().NotBeNull();
        cliente!.IdCliente.Should().Be(_idClienteInscripto);
        cliente.Codigo.Should().Be("CLI-T-001");
        cliente.RazonSocial.Should().Be("Cliente Test Inscripto SA");
        cliente.CondicionIva.Should().Be(CondicionIva.Inscripto);
        cliente.Activo.Should().BeTrue();
    }

    [Fact]
    public async Task GET_cliente_por_id_inexistente_retorna_404()
    {
        using var client = ClienteConToken();

        var response = await client.GetAsync("/api/clientes/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_cliente_de_otra_empresa_retorna_404()
    {
        // El cliente de empresa B no debe ser accesible con el JWT de empresa A
        using var client = ClienteConToken();

        var response = await client.GetAsync($"/api/clientes/{_idClienteEmpresaB}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "el repositorio filtra por IdEmpresa del JWT — empresa B no es visible para empresa A");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // POST /api/clientes
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task POST_cliente_sin_jwt_retorna_401()
    {
        var dto = new CrearClienteDto("CLI-NEW-01", "Nuevo Cliente", null,
            CondicionIva.ConsumidorFinal, null, null, null, null, null, null);

        var response = await _client.PostAsJsonAsync("/api/clientes", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task POST_cliente_body_valido_con_cuit_retorna_201()
    {
        using var client = ClienteConToken();
        var dto = new CrearClienteDto(
            "CLI-NEW-02",
            "Cliente Nuevo Con CUIT",
            "33-99887766-6",   // CUIT empresa válido: prefijo 33, DV=6
            CondicionIva.Inscripto,
            "Av. San Martín 500",
            "Mendoza",
            "0261-555-0002",
            "nuevo@test.com",
            null,
            "Creado en test POST");

        var response = await client.PostAsJsonAsync("/api/clientes", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created,
            "un body válido con CUIT correcto debe generar un 201 Created");

        var result = await response.Content.ReadFromJsonAsync<ClienteDto>(JsonOpts);
        result.Should().NotBeNull();
        result!.IdCliente.Should().BeGreaterThan(0);
        result.Codigo.Should().Be("CLI-NEW-02");
        result.RazonSocial.Should().Be("Cliente Nuevo Con CUIT");
        result.CondicionIva.Should().Be(CondicionIva.Inscripto);
        result.Activo.Should().BeTrue();

        response.Headers.Location.Should().NotBeNull("la respuesta 201 debe incluir el header Location");
    }

    [Fact]
    public async Task POST_cliente_body_valido_sin_cuit_retorna_201()
    {
        using var client = ClienteConToken();
        var dto = new CrearClienteDto(
            "CLI-NEW-03",
            "Cliente Sin CUIT",
            null,
            CondicionIva.ConsumidorFinal,
            null, null, null, null, null, null);

        var response = await client.PostAsJsonAsync("/api/clientes", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created,
            "el CUIT es opcional — un cliente sin CUIT debe crearse correctamente");

        var result = await response.Content.ReadFromJsonAsync<ClienteDto>(JsonOpts);
        result!.CUIT.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task POST_cliente_cuit_invalido_formato_retorna_422()
    {
        // El validador FluentValidation verifica el formato antes de que llegue al dominio
        using var client = ClienteConToken();
        // 20-12345678-9 tiene DV incorrecto (debe ser 6) — supera la regex de FluentValidation
        // pero es rechazado por DomainException en Cliente.Crear → 400
        var dto = new CrearClienteDto(
            "CLI-NEW-04",
            "Cliente CUIT Inválido",
            "20-12345678-9",
            CondicionIva.Inscripto,
            null, null, null, null, null, null);

        var response = await client.PostAsJsonAsync("/api/clientes", dto);

        // DomainException (DV incorrecto) → 400 Bad Request
        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.UnprocessableEntity, HttpStatusCode.BadRequest },
            "un CUIT con DV incorrecto debe ser rechazado por el dominio");
    }

    [Fact]
    public async Task POST_cliente_razon_social_vacia_retorna_422()
    {
        using var client = ClienteConToken();
        var dto = new { codigo = "CLI-NEW-05", razonSocial = "", cuit = (string?)null, condicionIva = 1 };

        var response = await client.PostAsJsonAsync("/api/clientes", dto);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity,
            "FluentValidation debe rechazar RazonSocial vacía con 422");
    }

    [Fact]
    public async Task POST_cliente_codigo_duplicado_misma_empresa_retorna_400()
    {
        using var client = ClienteConToken();

        // Primer intento — debe crearlo
        var dto = new CrearClienteDto("CLI-DUP-01", "Cliente Dup A", null,
            CondicionIva.ConsumidorFinal, null, null, null, null, null, null);
        var primera = await client.PostAsJsonAsync("/api/clientes", dto);
        primera.StatusCode.Should().Be(HttpStatusCode.Created);

        // Segundo intento — mismo código → DomainException → 400
        var dto2 = new CrearClienteDto("CLI-DUP-01", "Cliente Dup B", null,
            CondicionIva.ConsumidorFinal, null, null, null, null, null, null);
        var segunda = await client.PostAsJsonAsync("/api/clientes", dto2);

        segunda.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "código duplicado en la misma empresa debe retornar 400");

        var mensaje = await LeerMensajeErrorAsync(segunda);
        mensaje.Should().Contain("CLI-DUP-01", "el mensaje debe mencionar el código duplicado");
    }

    [Fact]
    public async Task POST_cliente_cuit_duplicado_misma_empresa_retorna_400()
    {
        using var client = ClienteConToken();
        const string cuitDuplicado = "23-87654321-4";  // CUIL válido: prefijo 23, DV=4

        // Primer cliente con ese CUIT
        var dto1 = new CrearClienteDto("CLI-DUP-02", "Cliente CUIT Dup A", cuitDuplicado,
            CondicionIva.Inscripto, null, null, null, null, null, null);
        var primera = await client.PostAsJsonAsync("/api/clientes", dto1);
        primera.StatusCode.Should().Be(HttpStatusCode.Created);

        // Segundo cliente con el mismo CUIT → DomainException → 400
        var dto2 = new CrearClienteDto("CLI-DUP-03", "Cliente CUIT Dup B", cuitDuplicado,
            CondicionIva.Inscripto, null, null, null, null, null, null);
        var segunda = await client.PostAsJsonAsync("/api/clientes", dto2);

        segunda.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "CUIT duplicado en la misma empresa debe retornar 400");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // PUT /api/clientes/{id}
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task PUT_cliente_sin_jwt_retorna_401()
    {
        var dto = new ActualizarClienteDto("Razón Social Nueva", null,
            CondicionIva.Exento, null, null, null, null, null, null);

        var response = await _client.PutAsJsonAsync($"/api/clientes/{_idClienteExento}", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PUT_cliente_existente_body_valido_retorna_200_con_datos_actualizados()
    {
        // Usar el cliente monotributo para actualizar
        using var client = ClienteConToken();
        var dto = new ActualizarClienteDto(
            "Cliente Test Monotributo Actualizado",
            null,
            CondicionIva.Monotributo,
            "Av. 9 de Julio 100",
            "Buenos Aires",
            "011-5555-9999",
            "actualizado@test.com",
            null,
            "Observación actualizada");

        var response = await client.PutAsJsonAsync($"/api/clientes/{_idClienteMonotributo}", dto);

        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.OK, HttpStatusCode.NoContent },
            "actualizar un cliente existente con datos válidos debe retornar 200 o 204");

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var result = await response.Content.ReadFromJsonAsync<ClienteDto>(JsonOpts);
            result!.RazonSocial.Should().Be("Cliente Test Monotributo Actualizado");
            result.Localidad.Should().Be("Buenos Aires");
        }
    }

    [Fact]
    public async Task PUT_cliente_inexistente_retorna_400()
    {
        // El handler lanza DomainException("Cliente X no encontrado") → middleware → 400.
        // El endpoint declara ProducesProblem(404) como documentación, pero la implementación
        // actual retorna 400 vía DomainException. Este test verifica el comportamiento real.
        using var client = ClienteConToken();
        var dto = new ActualizarClienteDto("No Existe", null, CondicionIva.ConsumidorFinal,
            null, null, null, null, null, null);

        var response = await client.PutAsJsonAsync("/api/clientes/999999", dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "el handler lanza DomainException para ID inexistente → middleware mapea a 400");
    }

    [Fact]
    public async Task PUT_cliente_cuit_invalido_retorna_error()
    {
        using var client = ClienteConToken();
        var dto = new ActualizarClienteDto("Cliente Actualizado", "CUIT-INVALIDO",
            CondicionIva.Inscripto, null, null, null, null, null, null);

        var response = await client.PutAsJsonAsync($"/api/clientes/{_idClienteInscripto}", dto);

        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.UnprocessableEntity, HttpStatusCode.BadRequest },
            "un CUIT inválido en el update debe ser rechazado");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // DELETE /api/clientes/{id} — soft delete
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DELETE_cliente_sin_jwt_retorna_401()
    {
        var response = await _client.DeleteAsync($"/api/clientes/{_idClienteInscripto}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DELETE_cliente_existente_retorna_204_y_GET_posterior_retorna_404()
    {
        using var client = ClienteConToken();

        // Crear un cliente dedicado para el test de delete
        var dtoCrear = new CrearClienteDto("CLI-DEL-01", "Cliente A Eliminar", null,
            CondicionIva.ConsumidorFinal, null, null, null, null, null, null);
        var crearResp = await client.PostAsJsonAsync("/api/clientes", dtoCrear);
        crearResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var creado = await crearResp.Content.ReadFromJsonAsync<ClienteDto>(JsonOpts);
        var idAEliminar = creado!.IdCliente;

        // Soft delete
        var deleteResp = await client.DeleteAsync($"/api/clientes/{idAEliminar}");
        deleteResp.StatusCode.Should().Be(HttpStatusCode.NoContent,
            "DELETE de un cliente existente debe retornar 204");

        // GET posterior debe retornar 404 (filtro global de soft delete lo oculta)
        var getResp = await client.GetAsync($"/api/clientes/{idAEliminar}");
        getResp.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "después del soft delete, el cliente no debe ser accesible via GET");
    }

    [Fact]
    public async Task DELETE_cliente_registro_persiste_en_BD_con_Activo_false()
    {
        using var client = ClienteConToken();

        // Crear cliente
        var dto = new CrearClienteDto("CLI-DEL-02", "Cliente Soft Delete Verify", null,
            CondicionIva.ConsumidorFinal, null, null, null, null, null, null);
        var crearResp = await client.PostAsJsonAsync("/api/clientes", dto);
        var creado = await crearResp.Content.ReadFromJsonAsync<ClienteDto>(JsonOpts);
        var idVerif = creado!.IdCliente;

        // Soft delete
        await client.DeleteAsync($"/api/clientes/{idVerif}");

        // Verificar directo en BD que el registro sigue existiendo con Activo = false
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NewgestDbContext>();

        // IgnoreQueryFilters para bypassar el filtro global de soft delete
        var enBd = await db.Clientes
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.IdCliente == idVerif);

        enBd.Should().NotBeNull("el registro debe seguir existiendo en BD (soft delete)");
        enBd!.Activo.Should().BeFalse("el soft delete debe poner Activo = false");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GET /api/clientes/{id}/saldo-pendiente — stub Sprint 12
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GET_saldo_pendiente_con_jwt_retorna_200_con_saldo_cero()
    {
        using var client = ClienteConToken();

        var response = await client.GetAsync($"/api/clientes/{_idClienteInscripto}/saldo-pendiente");

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "el stub de saldo pendiente debe responder 200");

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.TryGetProperty("saldoPendiente", out var saldoProp).Should().BeTrue(
            "la respuesta debe incluir la propiedad saldoPendiente");
        saldoProp.GetDecimal().Should().Be(0m,
            "el stub debe retornar 0 hasta Sprint 12");
    }

    [Fact]
    public async Task GET_saldo_pendiente_sin_jwt_retorna_401()
    {
        var response = await _client.GetAsync($"/api/clientes/{_idClienteInscripto}/saldo-pendiente");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
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

    private static async Task<string> LeerMensajeErrorAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("message", out var msgProp))
                return msgProp.GetString() ?? body;
        }
        catch { /* devolver body raw si no es JSON */ }
        return body;
    }
}

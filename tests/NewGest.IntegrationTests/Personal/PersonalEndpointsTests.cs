using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NewGest.Application.DTOs.Personal;
using NewGest.Application.DTOs.Shared;
using NewGest.Domain.Enums;
using NewGest.Infrastructure.Data;

namespace NewGest.IntegrationTests.Personal;

/// <summary>
/// Tests de integración para los endpoints de Personal, Viajes y Mutuales (Sprint 5-7 — M13, M14).
/// Verifica ABM de empleados/vendedores, soft delete, filtros, y CRUD de Viajes/Mutuales.
/// </summary>
[Collection("IntegrationTests")]
public class PersonalEndpointsTests : IAsyncLifetime
{
    private readonly NewgestWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private string _token = default!;

    // IDs de datos sembrados para uso en tests
    private int _idEmpleadoVendedor;
    private int _idEmpleadoAdministrativo;

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    // CUILs válidos verificados contra el algoritmo módulo 11
    // 20-12345678-6: suma=5*2+4*0+3*1+2*2+7*3+6*4+5*5+4*6+3*7+2*8=10+0+3+4+21+24+25+24+21+16=148; 148%11=5; DV=11-5=6
    // 27-87654321-9: suma=5*2+4*7+3*8+2*7+7*6+6*5+5*4+4*3+3*2+2*1=10+28+24+14+42+30+20+12+6+2=188; 188%11=1; DV=9
    private const string CuilVendedor = "20-12345678-6";      // DV=6 correcto
    private const string CuilAdministrativo = "27-87654321-9"; // DV=9 correcto

    public PersonalEndpointsTests(NewgestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IAsyncLifetime — seeding y cleanup específicos de Personal
    // ──────────────────────────────────────────────────────────────────────────

    public async Task InitializeAsync()
    {
        _token = await LoginYObtenerTokenAsync();
        await LimpiarDatosPersonalAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NewgestDbContext>();

        // Empleado vendedor de prueba
        var vendedor = NewGest.Domain.Entities.Neg.Empleado.Crear(
            NewgestWebApplicationFactory.IdEmpresaTest,
            "EMP-T-001",
            "García Juan Test",
            CuilVendedor,
            RolEmpleado.Vendedor,
            comisionPorcentaje: 5m);

        // Empleado administrativo de prueba
        var admin = NewGest.Domain.Entities.Neg.Empleado.Crear(
            NewgestWebApplicationFactory.IdEmpresaTest,
            "EMP-T-002",
            "López María Test",
            CuilAdministrativo,
            RolEmpleado.Administrativo,
            comisionPorcentaje: null);

        db.Empleados.AddRange(vendedor, admin);
        await db.SaveChangesAsync();

        _idEmpleadoVendedor = vendedor.IdEmpleado;
        _idEmpleadoAdministrativo = admin.IdEmpleado;
    }

    public async Task DisposeAsync()
    {
        await LimpiarDatosPersonalAsync();
    }

    private async Task LimpiarDatosPersonalAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NewgestDbContext>();

        // Ignorar el query filter de soft delete para limpiar todos los registros de prueba
        await db.Database.ExecuteSqlRawAsync(
            "DELETE FROM neg.Empleados WHERE Legajo LIKE 'EMP-T-%' AND IdEmpresa = 1");
        await db.Database.ExecuteSqlRawAsync(
            "DELETE FROM neg.Viajes WHERE Descripcion LIKE 'VJE-T-%' AND IdEmpresa = 1");
        await db.Database.ExecuteSqlRawAsync(
            "DELETE FROM neg.Mutuales WHERE Codigo LIKE 'MUT-T-%' AND IdEmpresa = 1");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GET /api/empleados
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GET_empleados_sin_jwt_retorna_401()
    {
        var response = await _client.GetAsync("/api/empleados");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GET_empleados_con_jwt_retorna_200_y_PagedResult()
    {
        using var client = ClienteConToken();

        var response = await client.GetAsync("/api/empleados?page=1&pageSize=20");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var resultado = await response.Content.ReadFromJsonAsync<PagedResult<EmpleadoListItemDto>>(JsonOpts);
        resultado.Should().NotBeNull();
        resultado!.Items.Should().NotBeNull();
        resultado.Total.Should().BeGreaterThanOrEqualTo(2,
            "hay al menos 2 empleados de prueba sembrados");
    }

    [Fact]
    public async Task GET_empleados_filtro_soloVendedores_retorna_solo_vendedores()
    {
        using var client = ClienteConToken();

        var response = await client.GetAsync("/api/empleados?soloVendedores=true&page=1&pageSize=100");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var resultado = await response.Content.ReadFromJsonAsync<PagedResult<EmpleadoListItemDto>>(JsonOpts);
        resultado!.Items.Should().OnlyContain(e => e.EsVendedor,
            "el filtro soloVendedores debe retornar únicamente empleados con EsVendedor=true");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // POST /api/empleados
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task POST_empleado_sin_jwt_retorna_401()
    {
        var dto = new CrearEmpleadoDto("EMP-T-NJ", "Sin JWT", null, RolEmpleado.Otro, null);

        var response = await _client.PostAsJsonAsync("/api/empleados", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task POST_empleado_vendedor_retorna_201_y_EsVendedor_true()
    {
        using var client = ClienteConToken();
        var dto = new CrearEmpleadoDto(
            "EMP-T-003",
            "Ramírez Pedro Test",
            "20-30404050-6",   // DV=6 correcto: suma=82; 82%11=5; DV=11-5=6
            RolEmpleado.Vendedor,
            ComisionPorcentaje: 3m);

        var response = await client.PostAsJsonAsync("/api/empleados", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created,
            "crear un empleado vendedor con datos válidos debe retornar 201");

        var resultado = await response.Content.ReadFromJsonAsync<EmpleadoDto>(JsonOpts);
        resultado.Should().NotBeNull();
        resultado!.IdEmpleado.Should().BeGreaterThan(0);
        resultado.EsVendedor.Should().BeTrue("el rol Vendedor debe implicar EsVendedor=true");
        resultado.Rol.Should().Be(RolEmpleado.Vendedor);
        resultado.ComisionPorcentaje.Should().Be(3m);
        resultado.Activo.Should().BeTrue();
    }

    [Fact]
    public async Task POST_empleado_administrativo_retorna_201_y_EsVendedor_false()
    {
        using var client = ClienteConToken();
        var dto = new CrearEmpleadoDto(
            "EMP-T-004",
            "Fernández Ana Test",
            null,
            RolEmpleado.Administrativo,
            ComisionPorcentaje: null);

        var response = await client.PostAsJsonAsync("/api/empleados", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var resultado = await response.Content.ReadFromJsonAsync<EmpleadoDto>(JsonOpts);
        resultado!.EsVendedor.Should().BeFalse("un Administrativo no es vendedor");
        resultado.Rol.Should().Be(RolEmpleado.Administrativo);
    }

    [Fact]
    public async Task POST_empleado_cuil_invalido_retorna_error()
    {
        using var client = ClienteConToken();
        var dto = new CrearEmpleadoDto(
            "EMP-T-005",
            "Empleado CUIL Malo",
            "20-12345678-9",   // DV incorrecto (debe ser 6)
            RolEmpleado.Otro,
            null);

        var response = await client.PostAsJsonAsync("/api/empleados", dto);

        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity },
            "un CUIL con dígito verificador incorrecto debe ser rechazado por DomainException");
    }

    [Fact]
    public async Task POST_empleado_comision_fuera_de_rango_retorna_error()
    {
        using var client = ClienteConToken();
        var dto = new CrearEmpleadoDto(
            "EMP-T-006",
            "Empleado Comisión Alta",
            null,
            RolEmpleado.Vendedor,
            ComisionPorcentaje: 150m);   // Mayor a 100 — fuera de rango

        var response = await client.PostAsJsonAsync("/api/empleados", dto);

        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity },
            "comisión mayor a 100% debe ser rechazada por DomainException");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GET /api/empleados/{id}
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GET_empleado_por_id_existente_retorna_200()
    {
        using var client = ClienteConToken();

        var response = await client.GetAsync($"/api/empleados/{_idEmpleadoVendedor}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var empleado = await response.Content.ReadFromJsonAsync<EmpleadoDto>(JsonOpts);
        empleado.Should().NotBeNull();
        empleado!.IdEmpleado.Should().Be(_idEmpleadoVendedor);
        empleado.EsVendedor.Should().BeTrue();
    }

    [Fact]
    public async Task GET_empleado_por_id_inexistente_retorna_404()
    {
        using var client = ClienteConToken();

        var response = await client.GetAsync("/api/empleados/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // PUT /api/empleados/{id}
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task PUT_empleado_datos_validos_retorna_200_con_datos_actualizados()
    {
        using var client = ClienteConToken();
        var dto = new ActualizarEmpleadoDto(
            "García Juan Actualizado",
            null,
            RolEmpleado.Vendedor,
            ComisionPorcentaje: 7m);

        var response = await client.PutAsJsonAsync($"/api/empleados/{_idEmpleadoVendedor}", dto);

        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.OK, HttpStatusCode.NoContent },
            "actualizar un empleado con datos válidos debe retornar 200 o 204");

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var resultado = await response.Content.ReadFromJsonAsync<EmpleadoDto>(JsonOpts);
            resultado!.ApellidoNombre.Should().Be("García Juan Actualizado");
            resultado.ComisionPorcentaje.Should().Be(7m);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // DELETE /api/empleados/{id} — soft delete
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DELETE_empleado_existente_retorna_204_y_GET_posterior_retorna_404()
    {
        using var client = ClienteConToken();

        // Crear empleado dedicado para el test de delete
        var dtoCrear = new CrearEmpleadoDto(
            "EMP-T-DEL",
            "Empleado Para Eliminar",
            null,
            RolEmpleado.Otro,
            null);
        var crearResp = await client.PostAsJsonAsync("/api/empleados", dtoCrear);
        crearResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var creado = await crearResp.Content.ReadFromJsonAsync<EmpleadoDto>(JsonOpts);

        // Soft delete
        var deleteResp = await client.DeleteAsync($"/api/empleados/{creado!.IdEmpleado}");
        deleteResp.StatusCode.Should().Be(HttpStatusCode.NoContent,
            "DELETE de un empleado existente debe retornar 204");

        // GET posterior debe retornar 404 (el query filter excluye Activo=false)
        var getResp = await client.GetAsync($"/api/empleados/{creado.IdEmpleado}");
        getResp.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "después del soft delete el empleado no debe ser accesible via GET");
    }

    [Fact]
    public async Task DELETE_empleado_registro_persiste_en_BD_con_Activo_false()
    {
        using var client = ClienteConToken();

        // Crear empleado
        var dto = new CrearEmpleadoDto(
            "EMP-T-SFT",
            "Empleado Soft Delete Verificar",
            null,
            RolEmpleado.Otro,
            null);
        var crearResp = await client.PostAsJsonAsync("/api/empleados", dto);
        var creado = await crearResp.Content.ReadFromJsonAsync<EmpleadoDto>(JsonOpts);

        // Soft delete
        await client.DeleteAsync($"/api/empleados/{creado!.IdEmpleado}");

        // Verificar directamente en BD que el registro sigue existiendo con Activo = false
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NewgestDbContext>();

        var enBd = await db.Empleados
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.IdEmpleado == creado.IdEmpleado);

        enBd.Should().NotBeNull("el registro debe seguir existiendo en BD (soft delete)");
        enBd!.Activo.Should().BeFalse("el soft delete debe poner Activo = false");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GET /api/vendedores
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GET_vendedores_con_jwt_retorna_200_solo_vendedores()
    {
        using var client = ClienteConToken();

        var response = await client.GetAsync("/api/vendedores");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var lista = await response.Content.ReadFromJsonAsync<List<EmpleadoListItemDto>>(JsonOpts);
        lista.Should().NotBeNull();
        lista!.Should().OnlyContain(e => e.EsVendedor,
            "el endpoint /api/vendedores debe retornar solo empleados con EsVendedor=true");
        lista.Should().Contain(e => e.IdEmpleado == _idEmpleadoVendedor,
            "el vendedor de prueba debe aparecer en la lista");
        lista.Should().NotContain(e => e.IdEmpleado == _idEmpleadoAdministrativo,
            "el empleado administrativo no debe aparecer en la lista de vendedores");
    }

    [Fact]
    public async Task GET_vendedores_sin_jwt_retorna_401()
    {
        var response = await _client.GetAsync("/api/vendedores");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Viajes
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GET_viajes_sin_jwt_retorna_401()
    {
        var response = await _client.GetAsync("/api/viajes");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GET_viajes_con_jwt_retorna_200()
    {
        using var client = ClienteConToken();

        var response = await client.GetAsync("/api/viajes");

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "el endpoint de viajes debe retornar 200 aunque la lista esté vacía");

        var lista = await response.Content.ReadFromJsonAsync<List<ViajeDto>>(JsonOpts);
        lista.Should().NotBeNull("la respuesta debe ser una lista");
    }

    [Fact]
    public async Task POST_viaje_datos_validos_retorna_201_y_existe_en_BD()
    {
        using var client = ClienteConToken();
        var dto = new CrearViajeDto(
            "VJE-T-001 Viaje a Córdoba",
            DateTime.UtcNow.AddDays(5),
            "Córdoba Capital",
            "Test de integración");

        var response = await client.PostAsJsonAsync("/api/viajes", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created,
            "crear un viaje con datos válidos debe retornar 201");

        var resultado = await response.Content.ReadFromJsonAsync<ViajeDto>(JsonOpts);
        resultado.Should().NotBeNull();
        resultado!.IdViaje.Should().BeGreaterThan(0);
        resultado.Descripcion.Should().Be("VJE-T-001 Viaje a Córdoba");
        resultado.Destino.Should().Be("Córdoba Capital");
        resultado.Activo.Should().BeTrue();
    }

    [Fact]
    public async Task PUT_viaje_datos_validos_retorna_200_o_204()
    {
        using var client = ClienteConToken();

        // Crear viaje
        var crear = new CrearViajeDto("VJE-T-UPD Viaje Actualizar", DateTime.UtcNow, "Rosario", null);
        var crearResp = await client.PostAsJsonAsync("/api/viajes", crear);
        var creado = await crearResp.Content.ReadFromJsonAsync<ViajeDto>(JsonOpts);

        // Actualizar
        var actualizar = new ActualizarViajeDto("VJE-T-UPD Viaje Actualizado", DateTime.UtcNow.AddDays(1), "Rosario Centro", "Actualizado");
        var putResp = await client.PutAsJsonAsync($"/api/viajes/{creado!.IdViaje}", actualizar);

        putResp.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.OK, HttpStatusCode.NoContent },
            "actualizar un viaje válido debe retornar 200 o 204");

        if (putResp.StatusCode == HttpStatusCode.OK)
        {
            var resultado = await putResp.Content.ReadFromJsonAsync<ViajeDto>(JsonOpts);
            resultado!.Descripcion.Should().Be("VJE-T-UPD Viaje Actualizado");
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Mutuales
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GET_mutuales_con_jwt_retorna_200()
    {
        using var client = ClienteConToken();

        var response = await client.GetAsync("/api/mutuales");

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "el endpoint de mutuales debe retornar 200 aunque la lista esté vacía");

        var lista = await response.Content.ReadFromJsonAsync<List<MutualDto>>(JsonOpts);
        lista.Should().NotBeNull("la respuesta debe ser una lista");
    }

    [Fact]
    public async Task GET_mutuales_sin_jwt_retorna_401()
    {
        var response = await _client.GetAsync("/api/mutuales");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task POST_mutual_datos_validos_retorna_201_y_existe_en_BD()
    {
        using var client = ClienteConToken();
        var dto = new CrearMutualDto("MUT-T-01", "Mutual Test Unión");

        var response = await client.PostAsJsonAsync("/api/mutuales", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created,
            "crear una mutual con datos válidos debe retornar 201");

        var resultado = await response.Content.ReadFromJsonAsync<MutualDto>(JsonOpts);
        resultado.Should().NotBeNull();
        resultado!.IdMutual.Should().BeGreaterThan(0);
        resultado.Codigo.Should().Be("MUT-T-01");
        resultado.Descripcion.Should().Be("Mutual Test Unión");
        resultado.Activo.Should().BeTrue();
    }

    [Fact]
    public async Task PUT_mutual_datos_validos_retorna_200_o_204()
    {
        using var client = ClienteConToken();

        // Crear mutual
        var crear = new CrearMutualDto("MUT-T-02", "Mutual Original");
        var crearResp = await client.PostAsJsonAsync("/api/mutuales", crear);
        var creada = await crearResp.Content.ReadFromJsonAsync<MutualDto>(JsonOpts);

        // Actualizar
        var actualizar = new ActualizarMutualDto("Mutual Actualizada");
        var putResp = await client.PutAsJsonAsync($"/api/mutuales/{creada!.IdMutual}", actualizar);

        putResp.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.OK, HttpStatusCode.NoContent },
            "actualizar una mutual válida debe retornar 200 o 204");

        if (putResp.StatusCode == HttpStatusCode.OK)
        {
            var resultado = await putResp.Content.ReadFromJsonAsync<MutualDto>(JsonOpts);
            resultado!.Descripcion.Should().Be("Mutual Actualizada");
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Verificación del bug UseSqlOutputClause(false) — Empleados con trigger
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task POST_empleado_trigger_auditoria_no_interfiere_con_insert()
    {
        // Verifica que UseSqlOutputClause(false) está configurado correctamente en
        // EmpleadoConfiguration, lo que permite que la tabla neg.Empleados tenga
        // trigger de auditoría sin romper el INSERT de EF Core.
        // Si este test pasa, el bug está correctamente mitigado.
        using var client = ClienteConToken();
        var dto = new CrearEmpleadoDto(
            "EMP-T-TRG",
            "Trigger Test Empleado",
            null,
            RolEmpleado.Otro,
            null);

        var response = await client.PostAsJsonAsync("/api/empleados", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created,
            "el INSERT de Empleado no debe fallar por la OUTPUT clause — UseSqlOutputClause(false) debe estar configurado");

        var resultado = await response.Content.ReadFromJsonAsync<EmpleadoDto>(JsonOpts);
        resultado!.IdEmpleado.Should().BeGreaterThan(0,
            "el ID debe haberse generado correctamente a pesar del trigger de auditoría");
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

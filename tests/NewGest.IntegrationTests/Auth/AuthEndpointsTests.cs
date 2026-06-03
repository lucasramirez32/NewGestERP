using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NewGest.Application.Common;
using NewGest.Application.DTOs;
using NewGest.Infrastructure.Data;
using NewGest.IntegrationTests.Helpers;

namespace NewGest.IntegrationTests.Auth;

/// <summary>
/// Tests de integración para los endpoints de autenticación (Sprint 1-2).
/// Usan NewGest_Test con datos reales de SQL Server — sin mocks de BD.
/// </summary>
[Collection("IntegrationTests")]
public class AuthEndpointsTests : IClassFixture<NewgestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly NewgestWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public AuthEndpointsTests(NewgestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GET /api/auth/empresas
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GET_api_auth_empresas_sin_autenticacion_retorna_200()
    {
        // El endpoint es público (AllowAnonymous) — necesario para el dropdown de login
        var response = await _client.GetAsync("/api/auth/empresas");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GET_api_auth_empresas_retorna_empresa_de_prueba()
    {
        var response = await _client.GetAsync("/api/auth/empresas");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var empresas = await response.Content.ReadFromJsonAsync<List<EmpresaDto>>(JsonOpts);
        empresas.Should().NotBeNull();
        empresas.Should().ContainSingle(e =>
            e.Nombre == NewgestWebApplicationFactory.NombreEmpresaTest &&
            e.IdEmpresa == NewgestWebApplicationFactory.IdEmpresaTest);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // POST /api/auth/login — casos exitosos
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task POST_login_credenciales_correctas_retorna_200_con_token()
    {
        var dto = new LoginDto(
            NewgestWebApplicationFactory.IdEmpresaTest,
            NewgestWebApplicationFactory.UsuarioAdmin,
            NewgestWebApplicationFactory.PasswordAdmin);

        var response = await _client.PostAsJsonAsync("/api/auth/login", dto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var resultado = await response.Content.ReadFromJsonAsync<LoginResultDto>(JsonOpts);
        resultado.Should().NotBeNull();
        resultado!.Token.Should().NotBeNullOrWhiteSpace();
        resultado.IdEmpresa.Should().Be(NewgestWebApplicationFactory.IdEmpresaTest);
        resultado.Nombre.Should().Be(NewgestWebApplicationFactory.NombreAdmin);
        resultado.DebeResetearPassword.Should().BeFalse();
    }

    [Fact]
    public async Task POST_login_credenciales_correctas_el_token_contiene_claims_correctas()
    {
        var dto = new LoginDto(
            NewgestWebApplicationFactory.IdEmpresaTest,
            NewgestWebApplicationFactory.UsuarioAdmin,
            NewgestWebApplicationFactory.PasswordAdmin);

        var response = await _client.PostAsJsonAsync("/api/auth/login", dto);
        var resultado = await response.Content.ReadFromJsonAsync<LoginResultDto>(JsonOpts);

        var token = resultado!.Token;
        var empresa = JwtTestHelper.ObtenerClaim(token, NewgestClaims.Empresa);
        var nombre = JwtTestHelper.ObtenerClaim(token, NewgestClaims.Nombre);
        var debeResetear = JwtTestHelper.ObtenerClaim(token, NewgestClaims.DebeResetear);
        var sub = JwtTestHelper.ObtenerClaim(token, JwtRegisteredClaimNames.Sub);

        empresa.Should().Be(NewgestWebApplicationFactory.IdEmpresaTest.ToString(),
            "el claim newgest:empresa debe contener el IdEmpresa del usuario");
        nombre.Should().Be(NewgestWebApplicationFactory.NombreAdmin,
            "el claim newgest:nombre debe contener el nombre completo");
        debeResetear.Should().Be("false",
            "el admin de prueba no debe resetear password");
        sub.Should().NotBeNullOrWhiteSpace("sub debe ser el ID del usuario");
    }

    [Fact]
    public async Task POST_login_usuario_con_debe_resetear_password_retorna_flag_en_true()
    {
        var dto = new LoginDto(
            NewgestWebApplicationFactory.IdEmpresaTest,
            NewgestWebApplicationFactory.UsuarioPrimerLogin,
            NewgestWebApplicationFactory.PasswordPrimerLogin);

        var response = await _client.PostAsJsonAsync("/api/auth/login", dto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var resultado = await response.Content.ReadFromJsonAsync<LoginResultDto>(JsonOpts);
        resultado!.DebeResetearPassword.Should().BeTrue(
            "el usuario 'primervez' tiene DebeResetearPassword = true en BD");

        var debeResetear = JwtTestHelper.ObtenerClaim(resultado.Token, NewgestClaims.DebeResetear);
        debeResetear.Should().Be("true", "el claim JWT debe reflejar DebeResetearPassword");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // POST /api/auth/login — casos de error
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task POST_login_password_incorrecta_retorna_400()
    {
        var dto = new LoginDto(
            NewgestWebApplicationFactory.IdEmpresaTest,
            NewgestWebApplicationFactory.UsuarioAdmin,
            "PasswordIncorrecta99");

        var response = await _client.PostAsJsonAsync("/api/auth/login", dto);

        // El middleware mapea DomainException → 400
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_login_usuario_inexistente_retorna_400()
    {
        var dto = new LoginDto(
            NewgestWebApplicationFactory.IdEmpresaTest,
            "usuarioquenoexiste",
            "Password1234");

        var response = await _client.PostAsJsonAsync("/api/auth/login", dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await LeerMensajeErrorAsync(response);
        body.Should().Contain("Credenciales incorrectas",
            "el mensaje de error no debe revelar si el usuario existe o no");
    }

    [Fact]
    public async Task POST_login_usuario_empresa_incorrecta_retorna_400()
    {
        // El usuario admin existe en empresa 1; intentar con empresa 99 → fallo
        var dto = new LoginDto(
            IdEmpresa: 99,
            NewgestWebApplicationFactory.UsuarioAdmin,
            NewgestWebApplicationFactory.PasswordAdmin);

        var response = await _client.PostAsJsonAsync("/api/auth/login", dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData(0, "admin", "Admin1234")]        // IdEmpresa = 0 → validación FluentValidation
    [InlineData(1, "", "Admin1234")]              // NombreUsuario vacío
    [InlineData(1, "admin", "")]                 // Password vacía
    public async Task POST_login_datos_invalidos_retorna_422(
        int idEmpresa, string usuario, string password)
    {
        var dto = new { idEmpresa, nombreUsuario = usuario, password };

        var response = await _client.PostAsJsonAsync("/api/auth/login", dto);

        // FluentValidation → 422 Unprocessable Entity
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // POST /api/auth/login — bloqueo por 5 intentos fallidos
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task POST_login_5_intentos_fallidos_bloquean_la_cuenta()
    {
        // Este test usa un usuario dedicado para no interferir con otros tests
        // El usuario se crea en el scope del test y se limpia al finalizar
        const string usuarioBloqueo = "usuario_bloqueo_test";
        const string passwordBloqueo = "Bloqueo1234";

        await CrearUsuarioTempAsync(usuarioBloqueo, passwordBloqueo);

        try
        {
            // 5 intentos con password incorrecta
            for (int i = 1; i <= 5; i++)
            {
                var dto = new LoginDto(
                    NewgestWebApplicationFactory.IdEmpresaTest,
                    usuarioBloqueo,
                    "PasswordMala99");
                await _client.PostAsJsonAsync("/api/auth/login", dto);
            }

            // El 6to intento — incluso con password correcta — debe retornar 400 "cuenta bloqueada"
            var intentoFinal = new LoginDto(
                NewgestWebApplicationFactory.IdEmpresaTest,
                usuarioBloqueo,
                passwordBloqueo);

            var response = await _client.PostAsJsonAsync("/api/auth/login", intentoFinal);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var mensaje = await LeerMensajeErrorAsync(response);
            mensaje.Should().Contain("bloqueada",
                "el mensaje debe indicar que la cuenta está bloqueada");
        }
        finally
        {
            await EliminarUsuarioTempAsync(usuarioBloqueo);
        }
    }

    [Fact]
    public async Task POST_login_cuenta_bloqueada_retorna_400_con_mensaje_especifico()
    {
        const string usuarioBloqueado = "usuario_ya_bloqueado";
        const string passwordBloqueado = "Bloqueado1234";

        await CrearUsuarioTempAsync(usuarioBloqueado, passwordBloqueado, bloqueado: true);

        try
        {
            var dto = new LoginDto(
                NewgestWebApplicationFactory.IdEmpresaTest,
                usuarioBloqueado,
                passwordBloqueado);

            var response = await _client.PostAsJsonAsync("/api/auth/login", dto);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var mensaje = await LeerMensajeErrorAsync(response);
            mensaje.Should().Contain("bloqueada");
            mensaje.Should().Contain("administrador",
                "el mensaje debe indicar que debe contactar al administrador");
        }
        finally
        {
            await EliminarUsuarioTempAsync(usuarioBloqueado);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GET /api/parametros — requiere JWT
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GET_api_parametros_sin_jwt_retorna_401()
    {
        var response = await _client.GetAsync("/api/parametros");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GET_api_parametros_con_jwt_valido_retorna_200()
    {
        var token = await LoginYObtenerTokenAsync();
        using var clientConToken = ClonarClienteConToken(token);

        var response = await clientConToken.GetAsync("/api/parametros");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var parametros = await response.Content.ReadFromJsonAsync<List<ParametroDto>>(JsonOpts);
        parametros.Should().NotBeNull("debe retornar una lista (puede estar vacía)");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // POST /api/auth/cambiar-password
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task POST_cambiar_password_sin_jwt_retorna_401()
    {
        var dto = new CambiarPasswordDto("vieja123", "Nueva5678");

        var response = await _client.PostAsJsonAsync("/api/auth/cambiar-password", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task POST_cambiar_password_con_jwt_valido_retorna_204()
    {
        // Usar el usuario "primervez" para el cambio — tiene DebeResetearPassword = true
        // Leer el usuario fresco y crear uno ad-hoc para no afectar otros tests
        const string usuarioCambio = "usuario_cambio_pwd";
        const string passwordOriginal = "Original1234";
        const string passwordNueva = "NuevaPass5678";

        await CrearUsuarioTempAsync(usuarioCambio, passwordOriginal);

        try
        {
            var token = await LoginYObtenerTokenAsync(usuarioCambio, passwordOriginal);
            using var clientConToken = ClonarClienteConToken(token);

            var dto = new CambiarPasswordDto(passwordOriginal, passwordNueva);
            var response = await clientConToken.PostAsJsonAsync("/api/auth/cambiar-password", dto);

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }
        finally
        {
            await EliminarUsuarioTempAsync(usuarioCambio);
        }
    }

    [Fact]
    public async Task POST_cambiar_password_despues_del_cambio_password_vieja_no_funciona()
    {
        const string usuarioCambio2 = "usuario_cambio_pwd2";
        const string passwordOriginal = "Original1234";
        const string passwordNueva = "NuevaPass5678";

        await CrearUsuarioTempAsync(usuarioCambio2, passwordOriginal);

        try
        {
            // 1. Cambiar password
            var token = await LoginYObtenerTokenAsync(usuarioCambio2, passwordOriginal);
            using var clientConToken = ClonarClienteConToken(token);
            var dtoChange = new CambiarPasswordDto(passwordOriginal, passwordNueva);
            var cambioResponse = await clientConToken.PostAsJsonAsync("/api/auth/cambiar-password", dtoChange);
            cambioResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // 2. Intentar login con password vieja → debe fallar
            var dtoLoginVieja = new LoginDto(
                NewgestWebApplicationFactory.IdEmpresaTest,
                usuarioCambio2,
                passwordOriginal);
            var loginViejaResponse = await _client.PostAsJsonAsync("/api/auth/login", dtoLoginVieja);

            loginViejaResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest,
                "la password anterior ya no debe ser válida tras el cambio");

            // 3. Login con password nueva → debe funcionar
            var dtoLoginNueva = new LoginDto(
                NewgestWebApplicationFactory.IdEmpresaTest,
                usuarioCambio2,
                passwordNueva);
            var loginNuevaResponse = await _client.PostAsJsonAsync("/api/auth/login", dtoLoginNueva);

            loginNuevaResponse.StatusCode.Should().Be(HttpStatusCode.OK,
                "la nueva password debe permitir el login");
        }
        finally
        {
            await EliminarUsuarioTempAsync(usuarioCambio2);
        }
    }

    [Theory]
    [InlineData("corta1")]          // Menos de 8 caracteres
    [InlineData("sinNumeros!!")]    // Sin dígitos
    public async Task POST_cambiar_password_password_nueva_invalida_retorna_422(string passwordNueva)
    {
        const string usuarioCambio3 = "usuario_cambio_inv";
        const string passwordOriginal = "Original1234";

        await CrearUsuarioTempAsync(usuarioCambio3, passwordOriginal);

        try
        {
            var token = await LoginYObtenerTokenAsync(usuarioCambio3, passwordOriginal);
            using var clientConToken = ClonarClienteConToken(token);

            var dto = new CambiarPasswordDto(passwordOriginal, passwordNueva);
            var response = await clientConToken.PostAsJsonAsync("/api/auth/cambiar-password", dto);

            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity,
                $"la password '{passwordNueva}' debe fallar la validación");
        }
        finally
        {
            await EliminarUsuarioTempAsync(usuarioCambio3);
        }
    }

    [Fact]
    public async Task POST_cambiar_password_actualiza_debe_resetear_password_a_false()
    {
        const string usuarioCambio4 = "usuario_resetear_test";
        const string passwordOriginal = "Reset1234";
        const string passwordNueva = "NuevoReset5678";

        // Crear usuario con DebeResetearPassword = true
        await CrearUsuarioTempAsync(usuarioCambio4, passwordOriginal, debeResetear: true);

        try
        {
            var loginResult = await LoginYObtenerResultadoAsync(usuarioCambio4, passwordOriginal);
            loginResult.DebeResetearPassword.Should().BeTrue("debe estar en true antes del cambio");

            using var clientConToken = ClonarClienteConToken(loginResult.Token);
            var dto = new CambiarPasswordDto(passwordOriginal, passwordNueva);
            await clientConToken.PostAsJsonAsync("/api/auth/cambiar-password", dto);

            // Volver a loguearse y verificar que DebeResetearPassword quedó en false
            var nuevoLogin = await LoginYObtenerResultadoAsync(usuarioCambio4, passwordNueva);
            nuevoLogin.DebeResetearPassword.Should().BeFalse(
                "después de cambiar password, DebeResetearPassword debe ser false");
        }
        finally
        {
            await EliminarUsuarioTempAsync(usuarioCambio4);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Auditoría — aud.EventLog
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AuditLog_al_crear_usuario_via_seeding_registra_INSERT_en_EventLog()
    {
        // La factory ya creó usuarios al inicializar la BD.
        // Verificar que el trigger registró al menos un INSERT en cfg.Users.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NewgestDbContext>();
        var conn = db.Database.GetDbConnection();

        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "SELECT COUNT(*) FROM aud.EventLog WHERE Tabla = 'Users' AND Schema_ = 'cfg' AND Accion = 'INSERT'";
        var count = (int)(await cmd.ExecuteScalarAsync() ?? 0);
        await conn.CloseAsync();

        count.Should().BeGreaterThan(0,
            "el trigger trg_Users_Audit debe registrar cada INSERT en cfg.Users en aud.EventLog");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Helpers privados
    // ──────────────────────────────────────────────────────────────────────────

    private async Task<string> LoginYObtenerTokenAsync(
        string? usuario = null,
        string? password = null)
    {
        var result = await LoginYObtenerResultadoAsync(
            usuario ?? NewgestWebApplicationFactory.UsuarioAdmin,
            password ?? NewgestWebApplicationFactory.PasswordAdmin);
        return result.Token;
    }

    private async Task<LoginResultDto> LoginYObtenerResultadoAsync(
        string usuario,
        string password)
    {
        var dto = new LoginDto(NewgestWebApplicationFactory.IdEmpresaTest, usuario, password);
        var response = await _client.PostAsJsonAsync("/api/auth/login", dto);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<LoginResultDto>(JsonOpts);
        return result!;
    }

    private HttpClient ClonarClienteConToken(string token)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private async Task CrearUsuarioTempAsync(
        string nombreUsuario,
        string password,
        bool bloqueado = false,
        bool debeResetear = false)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider
            .GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<NewGest.Domain.Entities.Users.ApplicationUser>>();

        // Si ya existe, eliminarlo primero
        var existente = await userManager.FindByNameAsync(nombreUsuario);
        if (existente != null)
            await userManager.DeleteAsync(existente);

        var user = new NewGest.Domain.Entities.Users.ApplicationUser
        {
            UserName = nombreUsuario,
            NormalizedUserName = nombreUsuario.ToUpper(),
            IdEmpresa = NewgestWebApplicationFactory.IdEmpresaTest,
            Nombre = $"Temp {nombreUsuario}",
            DebeResetearPassword = debeResetear,
            Bloqueado = bloqueado,
            IntentosFallidos = 0
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new InvalidOperationException(
                $"No se pudo crear usuario temporal {nombreUsuario}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
    }

    private async Task EliminarUsuarioTempAsync(string nombreUsuario)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider
            .GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<NewGest.Domain.Entities.Users.ApplicationUser>>();

        var user = await userManager.FindByNameAsync(nombreUsuario);
        if (user != null)
            await userManager.DeleteAsync(user);
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
        catch
        {
            // Si no es JSON válido, devolver el body raw
        }
        return body;
    }
}

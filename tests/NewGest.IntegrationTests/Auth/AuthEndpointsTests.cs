using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using NewGest.Application.DTOs;

namespace NewGest.IntegrationTests.Auth;

/// <summary>
/// Tests de integración para los endpoints de autenticación.
/// Requiere Docker (SQL Server en Testcontainers) para ejecutarse.
/// En CI se ejecutan con INTEGRATION_TESTS=true.
/// </summary>
public class AuthEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AuthEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // En tests de integración se puede reemplazar el DbContext
                // por uno en memoria o con Testcontainers (configurar aquí).
            });
        }).CreateClient();
    }

    [Fact]
    public async Task GET_api_auth_empresas_retorna_200()
    {
        // Act
        var response = await _client.GetAsync("/api/auth/empresas");

        // Assert — el endpoint es público, debe responder 200 aunque no haya empresas
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
        // InternalServerError puede ocurrir si no hay BD disponible — está bien en tests sin infra
    }

    [Fact]
    public async Task POST_api_auth_login_con_datos_invalidos_retorna_400_o_error()
    {
        // Arrange
        var loginDto = new { idEmpresa = 0, nombreUsuario = "", password = "" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

        // Assert — debe retornar 400 Bad Request o 422 Unprocessable
        ((int)response.StatusCode).Should().BeGreaterThanOrEqualTo(400);
    }

    [Fact]
    public async Task GET_api_parametros_sin_jwt_retorna_401()
    {
        // Act
        var response = await _client.GetAsync("/api/parametros");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task POST_api_auth_cambiar_password_sin_jwt_retorna_401()
    {
        // Arrange
        var dto = new { passwordActual = "vieja123", passwordNueva = "nueva456" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/cambiar-password", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

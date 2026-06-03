using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using NewGest.Application.Features.Auth.Commands.Login;
using NewGest.Application.Interfaces;
using NewGest.Domain.Common;
using NewGest.Domain.Entities.Users;

namespace NewGest.UnitTests.Auth;

public class LoginCommandHandlerTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<IJwtService> _jwtServiceMock;
    private readonly Mock<ILogger<LoginCommandHandler>> _loggerMock;
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        // UserManager requiere un store mockeado
        var store = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _jwtServiceMock = new Mock<IJwtService>();
        _loggerMock = new Mock<ILogger<LoginCommandHandler>>();

        _handler = new LoginCommandHandler(
            _userManagerMock.Object,
            _jwtServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Login_correcto_retorna_token_con_claims_correctas()
    {
        // Arrange
        var usuario = CrearUsuario(idEmpresa: 1, bloqueado: false, debeReset: false);
        _userManagerMock.Setup(m => m.FindByNameAsync("admin")).ReturnsAsync(usuario);
        _userManagerMock.Setup(m => m.CheckPasswordAsync(usuario, "Pass123")).ReturnsAsync(true);
        _userManagerMock.Setup(m => m.GetClaimsAsync(usuario)).ReturnsAsync(new List<Claim>());
        _userManagerMock.Setup(m => m.UpdateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success);
        _jwtServiceMock.Setup(j => j.GenerarToken(usuario, It.IsAny<IList<Claim>>()))
            .Returns("jwt-token-fake");

        var command = new LoginCommand(1, "admin", "Pass123");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Token.Should().Be("jwt-token-fake");
        result.IdEmpresa.Should().Be(1);
        result.DebeResetearPassword.Should().BeFalse();
    }

    [Fact]
    public async Task Login_incorrecto_incrementa_IntentosFallidos()
    {
        // Arrange
        var usuario = CrearUsuario(idEmpresa: 1, bloqueado: false, debeReset: true);
        usuario.IntentosFallidos = 0;

        _userManagerMock.Setup(m => m.FindByNameAsync("admin")).ReturnsAsync(usuario);
        _userManagerMock.Setup(m => m.CheckPasswordAsync(usuario, "wrong")).ReturnsAsync(false);
        _userManagerMock.Setup(m => m.UpdateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        Func<Task> act = () => _handler.Handle(new LoginCommand(1, "admin", "wrong"), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainException>().WithMessage("Credenciales incorrectas.");
        usuario.IntentosFallidos.Should().Be(1);
    }

    [Fact]
    public async Task Cinco_intentos_fallidos_bloquea_usuario()
    {
        // Arrange
        var usuario = CrearUsuario(idEmpresa: 1, bloqueado: false, debeReset: true);
        usuario.IntentosFallidos = 4; // ya tenía 4, el 5to bloqueará

        _userManagerMock.Setup(m => m.FindByNameAsync("admin")).ReturnsAsync(usuario);
        _userManagerMock.Setup(m => m.CheckPasswordAsync(usuario, "wrong")).ReturnsAsync(false);
        _userManagerMock.Setup(m => m.UpdateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        Func<Task> act = () => _handler.Handle(new LoginCommand(1, "admin", "wrong"), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainException>();
        usuario.Bloqueado.Should().BeTrue();
    }

    [Fact]
    public async Task Usuario_bloqueado_retorna_error_especifico_sin_validar_password()
    {
        // Arrange
        var usuario = CrearUsuario(idEmpresa: 1, bloqueado: true, debeReset: true);

        _userManagerMock.Setup(m => m.FindByNameAsync("admin")).ReturnsAsync(usuario);

        // Act
        Func<Task> act = () => _handler.Handle(new LoginCommand(1, "admin", "cualquier"), CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.Message.Should().Contain("bloqueada");

        // No debe intentar validar la contraseña
        _userManagerMock.Verify(m => m.CheckPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Usuario_empresa_incorrecta_retorna_credenciales_incorrectas()
    {
        // El usuario existe pero en otra empresa
        var usuario = CrearUsuario(idEmpresa: 2, bloqueado: false, debeReset: false);
        _userManagerMock.Setup(m => m.FindByNameAsync("admin")).ReturnsAsync(usuario);

        // Act — intenta login en empresa 1 pero el usuario es de empresa 2
        Func<Task> act = () => _handler.Handle(new LoginCommand(1, "admin", "Pass123"), CancellationToken.None);

        // Assert — no debe revelar que el usuario existe
        await act.Should().ThrowAsync<DomainException>().WithMessage("Credenciales incorrectas.");
    }

    [Fact]
    public async Task Usuario_no_encontrado_retorna_credenciales_incorrectas()
    {
        _userManagerMock.Setup(m => m.FindByNameAsync("noexiste")).ReturnsAsync((ApplicationUser?)null);

        Func<Task> act = () => _handler.Handle(new LoginCommand(1, "noexiste", "Pass123"), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("Credenciales incorrectas.");
    }

    [Fact]
    public async Task Login_correcto_resetea_IntentosFallidos_y_actualiza_UltimoLogin()
    {
        // Arrange
        var usuario = CrearUsuario(idEmpresa: 1, bloqueado: false, debeReset: false);
        usuario.IntentosFallidos = 3;

        _userManagerMock.Setup(m => m.FindByNameAsync("admin")).ReturnsAsync(usuario);
        _userManagerMock.Setup(m => m.CheckPasswordAsync(usuario, "Pass123")).ReturnsAsync(true);
        _userManagerMock.Setup(m => m.GetClaimsAsync(usuario)).ReturnsAsync(new List<Claim>());
        _userManagerMock.Setup(m => m.UpdateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success);
        _jwtServiceMock.Setup(j => j.GenerarToken(usuario, It.IsAny<IList<Claim>>())).Returns("token");

        // Act
        await _handler.Handle(new LoginCommand(1, "admin", "Pass123"), CancellationToken.None);

        // Assert
        usuario.IntentosFallidos.Should().Be(0);
        usuario.UltimoLogin.Should().NotBeNull();
        usuario.UltimoLogin!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    // ─── Helpers ────────────────────────────────────────────────────────────
    private static ApplicationUser CrearUsuario(int idEmpresa, bool bloqueado, bool debeReset) => new()
    {
        Id = 1,
        UserName = "admin",
        Nombre = "Admin Test",
        IdEmpresa = idEmpresa,
        Bloqueado = bloqueado,
        DebeResetearPassword = debeReset
    };
}

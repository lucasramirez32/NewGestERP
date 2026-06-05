using FluentAssertions;
using Moq;
using NewGest.Application.DTOs.Contabilidad;
using NewGest.Application.Features.Contabilidad.Commands.CrearAsiento;
using NewGest.Application.Interfaces;
using NewGest.Domain.Common;
using NewGest.Domain.Entities.Cnt;
using NewGest.Domain.Enums;

namespace NewGest.UnitTests.Contabilidad;

public class CrearAsientoCommandHandlerTests
{
    private readonly Mock<IAsientoRepository>     _asientosRepo = new();
    private readonly Mock<ICuentaContableRepository> _cuentasRepo = new();
    private readonly Mock<IUnitOfWork>            _uow          = new();

    private CrearAsientoCommandHandler Handler() =>
        new(_asientosRepo.Object, _cuentasRepo.Object, _uow.Object);

    private static CuentaContable CuentaImputable(int id, string codigo)
    {
        var c = CuentaContable.Crear(1, codigo, $"Cuenta {codigo}",
            null, NaturalezaCuenta.Deudora, TipoCuenta.Activo, imputaDirectamente: true);
        // Forzar IdCuenta via reflexión (setter privado)
        typeof(CuentaContable).GetProperty("IdCuenta")!
            .SetValue(c, id, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance, null, null, null);
        return c;
    }

    private void ConfigurarBase()
    {
        _asientosRepo.Setup(r => r.ObtenerProximoNumeroAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(10L);
        _asientosRepo.Setup(r => r.AddAsync(It.IsAny<Asiento>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _uow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private static CrearAsientoCommand CommandEquilibrado() => new(IdEmpresa: 1, Datos: new CrearAsientoDto(
        Fecha: new DateOnly(2026, 6, 5),
        Descripcion: "Ajuste manual",
        Partidas: [
            new CrearPartidaDto(IdCuenta: 10, Debe: 500m, Haber: 0,    Concepto: "Débito"),
            new CrearPartidaDto(IdCuenta: 20, Debe: 0,    Haber: 500m, Concepto: "Crédito"),
        ]
    ));

    // ─── Happy path ───────────────────────────────────────────────────────────
    [Fact]
    public async Task Asiento_equilibrado_persiste_y_retorna_dto()
    {
        _cuentasRepo.Setup(r => r.GetByIdAsync(10, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CuentaContable.Crear(1, "1.1", "Caja", null,
                NaturalezaCuenta.Deudora, TipoCuenta.Activo, true));
        _cuentasRepo.Setup(r => r.GetByIdAsync(20, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CuentaContable.Crear(1, "2.1", "Pasivo", null,
                NaturalezaCuenta.Acreedora, TipoCuenta.Pasivo, true));
        ConfigurarBase();

        var result = await Handler().Handle(CommandEquilibrado(), CancellationToken.None);

        result.TotalDebe.Should().Be(500m);
        result.TotalHaber.Should().Be(500m);
        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─── Cuenta inexistente ───────────────────────────────────────────────────
    [Fact]
    public async Task Cuenta_inexistente_lanza_DomainException()
    {
        _cuentasRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>(), 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CuentaContable?)null);

        var act = () => Handler().Handle(CommandEquilibrado(), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*no encontrada*");
    }

    // ─── Cuenta agrupadora ────────────────────────────────────────────────────
    [Fact]
    public async Task Cuenta_agrupadora_lanza_DomainException()
    {
        var agrupadora = CuentaContable.Crear(1, "1", "Activo Total",
            null, NaturalezaCuenta.Deudora, TipoCuenta.Activo, imputaDirectamente: false);

        _cuentasRepo.Setup(r => r.GetByIdAsync(10, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(agrupadora);
        _cuentasRepo.Setup(r => r.GetByIdAsync(20, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(agrupadora);

        var act = () => Handler().Handle(CommandEquilibrado(), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*agrupadora*");
    }

    // ─── Asiento desequilibrado detectado en el handler ──────────────────────
    [Fact]
    public async Task Asiento_desequilibrado_lanza_DomainException()
    {
        _cuentasRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>(), 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CuentaContable.Crear(1, "1.1", "X", null,
                NaturalezaCuenta.Deudora, TipoCuenta.Activo, true));
        _asientosRepo.Setup(r => r.ObtenerProximoNumeroAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1L);

        var cmdDesequilibrado = new CrearAsientoCommand(1, new CrearAsientoDto(
            new DateOnly(2026, 6, 5), "Test",
            [
                new CrearPartidaDto(10, 500m, 0,    null),
                new CrearPartidaDto(20, 0,    400m, null),  // diferencia de $100
            ]));

        var act = () => Handler().Handle(cmdDesequilibrado, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*desequilibrado*");
        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}

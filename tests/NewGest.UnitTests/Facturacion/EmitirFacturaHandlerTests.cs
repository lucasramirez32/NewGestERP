using FluentAssertions;
using Moq;
using NewGest.Application.DTOs.Comprobantes;
using NewGest.Application.Features.Comprobantes.Commands.EmitirFactura;
using NewGest.Application.Interfaces;
using NewGest.Application.Services;
using NewGest.Domain.Common;
using NewGest.Domain.Entities.Com;
using NewGest.Domain.Entities.Empresas;
using NewGest.Domain.Entities.Neg;
using NewGest.Domain.Enums;

namespace NewGest.UnitTests.Facturacion;

public class EmitirFacturaHandlerTests
{
    // ─── Mocks ────────────────────────────────────────────────────────────────
    private readonly Mock<IComprobanteRepository> _comprobantesRepo = new();
    private readonly Mock<IClienteRepository>     _clientesRepo     = new();
    private readonly Mock<IEmpresaRepository>     _empresasRepo     = new();
    private readonly Mock<IStockService>          _stockService     = new();
    private readonly Mock<IAfipService>           _afipService      = new();
    private readonly Mock<IUnitOfWork>            _uow              = new();

    private EmitirFacturaCommandHandler ConstruirHandler() => new(
        _comprobantesRepo.Object,
        _clientesRepo.Object,
        _empresasRepo.Object,
        _stockService.Object,
        _afipService.Object,
        _uow.Object);

    private void ConfigurarEmpresaYCliente(CondicionIva condicionCliente = CondicionIva.ConsumidorFinal)
    {
        _empresasRepo.Setup(r => r.ObtenerPorIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Empresa { IdEmpresa = 1, Nombre = "Test SA", Cuit = "20111222333" });

        var cliente = Cliente.Crear(
            idEmpresa: 1, codigo: "CLI001", razonSocial: "Juan Pérez",
            cuit: null, condicionIva: condicionCliente,
            domicilio: null, localidad: null, telefono: null, email: null, idZona: null, observaciones: null);

        _clientesRepo.Setup(r => r.GetByIdAsync(10, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cliente);

        _comprobantesRepo.Setup(r => r.ObtenerProximoNumeroAsync(1, 1, It.IsAny<TipoComprobante>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(42L);

        _comprobantesRepo.Setup(r => r.AddAsync(It.IsAny<Comprobante>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _stockService.Setup(s => s.DescontarStockAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _uow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }

    private static EmitirFacturaCommand CommandFacturaB() => new(
        IdEmpresa: 1, Tipo: TipoComprobante.FacturaB, PuntoVenta: 1,
        Fecha: DateOnly.FromDateTime(DateTime.Today), IdCliente: 10,
        IdPedidoOrigen: null,
        Items: [new ItemFacturaDto(5, "Producto", 2m, 500m, AlicuotaIva.Porcentaje21)]);

    // ─── Happy path ───────────────────────────────────────────────────────────
    [Fact]
    public async Task EmitirFacturaB_llama_AFIP_y_retorna_CAE()
    {
        ConfigurarEmpresaYCliente();
        _afipService.Setup(a => a.SolicitarCaeAsync(It.IsAny<ComprobanteAfip>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CaeResponse("12345678901234", new DateOnly(2026, 8, 31), "42"));

        var resultado = await ConstruirHandler().Handle(CommandFacturaB(), CancellationToken.None);

        resultado.CodigoCae.Should().Be("12345678901234");
        resultado.Numero.Should().Be(42);
        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EmitirFacturaB_descuenta_stock_antes_de_commit()
    {
        ConfigurarEmpresaYCliente();
        _afipService.Setup(a => a.SolicitarCaeAsync(It.IsAny<ComprobanteAfip>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CaeResponse("11111111111111", new DateOnly(2026, 8, 31), "42"));

        var secuencia = new MockSequence();
        _comprobantesRepo.InSequence(secuencia)
            .Setup(r => r.AddAsync(It.IsAny<Comprobante>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _stockService.InSequence(secuencia)
            .Setup(s => s.DescontarStockAsync(1, 5, 2m, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _uow.InSequence(secuencia)
            .Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        await ConstruirHandler().Handle(CommandFacturaB(), CancellationToken.None);

        _stockService.Verify(s => s.DescontarStockAsync(1, 5, 2m, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─── Rechazo AFIP: sin persistencia ni stock decrementado ─────────────────
    [Fact]
    public async Task Rechazo_AFIP_no_persiste_comprobante_ni_decrementa_stock()
    {
        ConfigurarEmpresaYCliente();
        _afipService.Setup(a => a.SolicitarCaeAsync(It.IsAny<ComprobanteAfip>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DomainException("AFIP rechazó: código 10016, tipo de cambio inválido."));

        var act = () => ConstruirHandler().Handle(CommandFacturaB(), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*10016*");

        // El comprobante NO debe haberse persistido
        _comprobantesRepo.Verify(r => r.AddAsync(It.IsAny<Comprobante>(), It.IsAny<CancellationToken>()), Times.Never);
        // El stock NO debe haberse decrementado
        _stockService.Verify(s => s.DescontarStockAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()), Times.Never);
        // No debe haber commit
        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ─── Empresa sin CUIT ─────────────────────────────────────────────────────
    [Fact]
    public async Task Empresa_sin_CUIT_lanza_DomainException()
    {
        _empresasRepo.Setup(r => r.ObtenerPorIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Empresa { IdEmpresa = 1, Nombre = "Sin CUIT", Cuit = null });

        var act = () => ConstruirHandler().Handle(CommandFacturaB(), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*CUIT*");
    }

    // ─── Recibos no llaman a AFIP ─────────────────────────────────────────────
    [Fact]
    public async Task ReciboB_no_llama_a_AFIP()
    {
        ConfigurarEmpresaYCliente();
        var cmd = CommandFacturaB() with { Tipo = TipoComprobante.RecibosB };

        await ConstruirHandler().Handle(cmd, CancellationToken.None);

        _afipService.Verify(a => a.SolicitarCaeAsync(It.IsAny<ComprobanteAfip>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

using FluentAssertions;
using Moq;
using NewGest.Application.Features.Contabilidad.EventHandlers;
using NewGest.Application.Interfaces;
using NewGest.Domain.Common;
using NewGest.Domain.Entities.Com;
using NewGest.Domain.Entities.Cnt;
using NewGest.Domain.Entities.Config;
using NewGest.Domain.Enums;

namespace NewGest.UnitTests.Contabilidad;

public class GenerarAsientoFacturaHandlerTests
{
    private readonly Mock<IComprobanteRepository> _comprobantesRepo = new();
    private readonly Mock<IAsientoRepository>     _asientosRepo     = new();
    private readonly Mock<IParametroRepository>   _parametrosRepo   = new();

    private GenerarAsientoFacturaHandler Handler() => new(
        _comprobantesRepo.Object,
        _asientosRepo.Object,
        _parametrosRepo.Object);

    private static readonly CaeAsignadoNotification NotifFacturaB =
        new(IdComprobante: 1, IdEmpresa: 1, CodigoCae: "12345678901234");

    private static Comprobante FacturaB(decimal totalNeto, decimal totalIva)
    {
        var item = ItemComprobante.Crear(1, "Prod", 1m, totalNeto, AlicuotaIva.Porcentaje21);
        return Comprobante.Crear(
            idEmpresa: 1, tipo: TipoComprobante.FacturaB, puntoVenta: 1, numero: 42,
            fecha: new DateOnly(2026, 6, 5), idCliente: 10,
            razonSocialCliente: "Test SA", cuitCliente: null,
            condicionIvaReceptor: CondicionIva.ConsumidorFinal,
            items: [item]);
    }

    private void ConfigurarParametros()
    {
        _parametrosRepo.Setup(r => r.ObtenerPorClaveAsync("CUENTA_CXC",       1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Parametro { Clave = "CUENTA_CXC",       Valor = "11" });
        _parametrosRepo.Setup(r => r.ObtenerPorClaveAsync("CUENTA_VENTAS",    1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Parametro { Clave = "CUENTA_VENTAS",    Valor = "41" });
        _parametrosRepo.Setup(r => r.ObtenerPorClaveAsync("CUENTA_IVA_VENTAS", 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Parametro { Clave = "CUENTA_IVA_VENTAS", Valor = "24" });

        _asientosRepo.Setup(r => r.ObtenerProximoNumeroAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(100L);
        _asientosRepo.Setup(r => r.AddAsync(It.IsAny<Asiento>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    // ─── Happy path FC-B ──────────────────────────────────────────────────────
    [Fact]
    public async Task FacturaB_1000_neto_genera_asiento_equilibrado_CxC_Ventas_IVA()
    {
        var comp = FacturaB(1000m, 210m);
        _comprobantesRepo.Setup(r => r.GetByIdAsync(1, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(comp);
        ConfigurarParametros();

        await Handler().Handle(NotifFacturaB, CancellationToken.None);

        _asientosRepo.Verify(r => r.AddAsync(It.Is<Asiento>(a =>
            a.TotalDebe == 1210m &&
            a.TotalHaber == 1210m &&
            a.Partidas.Any(p => p.IdCuenta == 11 && p.Debe == 1210m) &&
            a.Partidas.Any(p => p.IdCuenta == 41 && p.Haber == 1000m) &&
            a.Partidas.Any(p => p.IdCuenta == 24 && p.Haber == 210m)
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─── Factura exenta (TotalIva = 0): solo 2 partidas ─────────────────────
    [Fact]
    public async Task FacturaB_exenta_genera_asiento_sin_partida_iva()
    {
        var item = ItemComprobante.Crear(1, "Prod exento", 1m, 500m, AlicuotaIva.Exento);
        var comp = Comprobante.Crear(1, TipoComprobante.FacturaB, 1, 43,
            new DateOnly(2026, 6, 5), 10, "Test SA", null, CondicionIva.ConsumidorFinal, [item]);

        _comprobantesRepo.Setup(r => r.GetByIdAsync(1, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(comp);
        ConfigurarParametros();

        await Handler().Handle(NotifFacturaB, CancellationToken.None);

        _asientosRepo.Verify(r => r.AddAsync(It.Is<Asiento>(a =>
            a.Partidas.Count == 2 &&
            a.Partidas.All(p => p.IdCuenta != 24)  // sin partida IVA
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─── Nota de Crédito: asiento inverso ────────────────────────────────────
    [Fact]
    public async Task NotaCreditoB_genera_asiento_inverso_Debe_Ventas_Haber_CxC()
    {
        var item = ItemComprobante.Crear(1, "NC", 1m, 1000m, AlicuotaIva.Porcentaje21);
        var comp = Comprobante.Crear(1, TipoComprobante.NotaCreditoB, 1, 5,
            new DateOnly(2026, 6, 5), 10, "Test SA", null, CondicionIva.ConsumidorFinal, [item]);

        _comprobantesRepo.Setup(r => r.GetByIdAsync(1, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(comp);
        ConfigurarParametros();

        await Handler().Handle(NotifFacturaB, CancellationToken.None);

        _asientosRepo.Verify(r => r.AddAsync(It.Is<Asiento>(a =>
            a.TotalDebe == 1210m &&
            a.TotalHaber == 1210m &&
            a.Partidas.Any(p => p.IdCuenta == 41 && p.Debe == 1000m)  && // Debe Ventas
            a.Partidas.Any(p => p.IdCuenta == 24 && p.Debe == 210m)   && // Debe IVA
            a.Partidas.Any(p => p.IdCuenta == 11 && p.Haber == 1210m)    // Haber CxC
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─── Bug #3: redondeo centavo — items con IVA 10.5% fracciones ───────────
    [Fact]
    public async Task Asiento_con_IVA105_fraccional_cuadra_centavo_a_centavo()
    {
        // 3 items × $333.33 con IVA 10.5% → IVA fraccional por ítem
        var items = new List<ItemComprobante>
        {
            ItemComprobante.Crear(1, "A", 3m, 333.33m, AlicuotaIva.Porcentaje10_5),
        };
        var comp = Comprobante.Crear(1, TipoComprobante.FacturaB, 1, 99,
            new DateOnly(2026, 6, 5), 10, "Test SA", null, CondicionIva.ConsumidorFinal, items);

        _comprobantesRepo.Setup(r => r.GetByIdAsync(1, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(comp);
        ConfigurarParametros();

        await Handler().Handle(NotifFacturaB, CancellationToken.None);

        _asientosRepo.Verify(r => r.AddAsync(It.Is<Asiento>(a =>
            a.TotalDebe == a.TotalHaber  // siempre debe cuadrar, sin importar el redondeo
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─── Parámetro no configurado ─────────────────────────────────────────────
    [Fact]
    public async Task Parametro_no_configurado_lanza_DomainException()
    {
        var comp = FacturaB(1000m, 210m);
        _comprobantesRepo.Setup(r => r.GetByIdAsync(1, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(comp);
        _parametrosRepo.Setup(r => r.ObtenerPorClaveAsync(It.IsAny<string>(), 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Parametro?)null);
        _asientosRepo.Setup(r => r.ObtenerProximoNumeroAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1L);

        var act = () => Handler().Handle(NotifFacturaB, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*no configurado*");
    }

    // ─── Comprobante no existe: silencioso ────────────────────────────────────
    [Fact]
    public async Task Comprobante_no_existe_no_lanza_no_crea_asiento()
    {
        _comprobantesRepo.Setup(r => r.GetByIdAsync(1, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Comprobante?)null);

        await Handler().Handle(NotifFacturaB, CancellationToken.None);

        _asientosRepo.Verify(r => r.AddAsync(It.IsAny<Asiento>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

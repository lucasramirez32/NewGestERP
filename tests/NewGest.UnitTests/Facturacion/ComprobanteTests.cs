using FluentAssertions;
using NewGest.Domain.Common;
using NewGest.Domain.Entities.Com;
using NewGest.Domain.Enums;

namespace NewGest.UnitTests.Facturacion;

public class ComprobanteTests
{
    // ─── Helpers ──────────────────────────────────────────────────────────────
    private static List<ItemComprobante> ItemsEjemplo(
        decimal cantidad = 10m, decimal precio = 100m, AlicuotaIva alicuota = AlicuotaIva.Porcentaje21)
        => [ItemComprobante.Crear(1, "Artículo test", cantidad, precio, alicuota)];

    private static Comprobante ComprobanteEjemplo(
        TipoComprobante tipo = TipoComprobante.FacturaB,
        CondicionIva condicion = CondicionIva.ConsumidorFinal,
        List<ItemComprobante>? items = null)
        => Comprobante.Crear(
            idEmpresa: 1,
            tipo: tipo,
            puntoVenta: 1,
            numero: 1,
            fecha: new DateOnly(2026, 6, 3),
            idCliente: 10,
            razonSocialCliente: "Juan Pérez",
            cuitCliente: null,
            condicionIvaReceptor: condicion,
            items: items ?? ItemsEjemplo());

    // ─── Cálculo IVA 21% ─────────────────────────────────────────────────────
    [Fact]
    public void ItemComprobante_IVA21_base1000_retorna_IVA210_total1210()
    {
        var item = ItemComprobante.Crear(1, "Prod", 1m, 1000m, AlicuotaIva.Porcentaje21);

        item.SubtotalNeto.Should().Be(1000m);
        item.Iva.Should().Be(210m);
        item.Subtotal.Should().Be(1210m);
    }

    [Fact]
    public void ItemComprobante_IVA105_base1000_retorna_IVA105_total1105()
    {
        var item = ItemComprobante.Crear(1, "Prod", 1m, 1000m, AlicuotaIva.Porcentaje10_5);

        item.SubtotalNeto.Should().Be(1000m);
        item.Iva.Should().Be(105m);
        item.Subtotal.Should().Be(1105m);
    }

    [Fact]
    public void ItemComprobante_Exento_total_igual_a_base()
    {
        var item = ItemComprobante.Crear(1, "Prod", 1m, 1000m, AlicuotaIva.Exento);

        item.Iva.Should().Be(0m);
        item.Subtotal.Should().Be(1000m);
    }

    [Fact]
    public void ItemComprobante_IVA0_total_igual_a_base()
    {
        var item = ItemComprobante.Crear(1, "Prod", 1m, 1000m, AlicuotaIva.Porcentaje0);

        item.Iva.Should().Be(0m);
        item.Subtotal.Should().Be(1000m);
    }

    // ─── Totales del comprobante ──────────────────────────────────────────────
    [Fact]
    public void Comprobante_total_suma_correctamente_multiples_items()
    {
        var items = new List<ItemComprobante>
        {
            ItemComprobante.Crear(1, "A", 5m, 100m, AlicuotaIva.Porcentaje21),   // neto 500, iva 105
            ItemComprobante.Crear(2, "B", 2m, 200m, AlicuotaIva.Porcentaje10_5), // neto 400, iva 42
        };

        var comp = ComprobanteEjemplo(items: items);

        comp.TotalNeto.Should().Be(900m);
        comp.TotalIva.Should().Be(147m);
        comp.Total.Should().Be(1047m);
    }

    // ─── Reglas de negocio tipo comprobante vs condición IVA ─────────────────
    [Fact]
    public void Comprobante_FacturaA_a_ConsumidorFinal_lanza_DomainException()
    {
        var act = () => ComprobanteEjemplo(TipoComprobante.FacturaA, CondicionIva.ConsumidorFinal);

        act.Should().Throw<DomainException>()
            .WithMessage("*Responsables Inscriptos*");
    }

    [Fact]
    public void Comprobante_FacturaA_a_Monotributo_lanza_DomainException()
    {
        var act = () => ComprobanteEjemplo(TipoComprobante.FacturaA, CondicionIva.Monotributo);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Comprobante_FacturaA_a_Inscripto_es_valido()
    {
        var comp = ComprobanteEjemplo(TipoComprobante.FacturaA, CondicionIva.Inscripto);

        comp.Tipo.Should().Be(TipoComprobante.FacturaA);
    }

    [Fact]
    public void Comprobante_FacturaB_a_ConsumidorFinal_es_valido()
    {
        var comp = ComprobanteEjemplo(TipoComprobante.FacturaB, CondicionIva.ConsumidorFinal);

        comp.Tipo.Should().Be(TipoComprobante.FacturaB);
    }

    [Fact]
    public void Comprobante_sin_items_lanza_DomainException()
    {
        var act = () => ComprobanteEjemplo(items: []);

        act.Should().Throw<DomainException>()
            .WithMessage("*al menos un ítem*");
    }

    // ─── AsignarCae ───────────────────────────────────────────────────────────
    [Fact]
    public void AsignarCae_asigna_correctamente_y_emite_domain_event()
    {
        var comp = ComprobanteEjemplo();
        comp.EsElectronica.Should().BeFalse();

        comp.AsignarCae("12345678901234", new DateOnly(2026, 8, 31));

        comp.EsElectronica.Should().BeTrue();
        comp.Cae!.Codigo.Should().Be("12345678901234");
        comp.Cae.FechaVencimiento.Should().Be(new DateOnly(2026, 8, 31));
        comp.DomainEvents.Should().ContainSingle(e =>
            e.GetType().Name == "CaeAsignadoEvent");
    }

    [Fact]
    public void AsignarCae_dos_veces_lanza_DomainException()
    {
        var comp = ComprobanteEjemplo();
        comp.AsignarCae("11111111111111", new DateOnly(2026, 8, 31));

        var act = () => comp.AsignarCae("22222222222222", new DateOnly(2026, 9, 30));

        act.Should().Throw<DomainException>()
            .WithMessage("*ya tiene un CAE*");
    }

    // ─── Anular ───────────────────────────────────────────────────────────────
    [Fact]
    public void Anular_cambia_flag_y_segunda_vez_lanza_DomainException()
    {
        var comp = ComprobanteEjemplo();
        comp.Anular();
        comp.Anulado.Should().BeTrue();

        var act = () => comp.Anular();
        act.Should().Throw<DomainException>().WithMessage("*ya está anulado*");
    }
}

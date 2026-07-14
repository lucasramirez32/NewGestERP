using FluentAssertions;
using NewGest.Application.Features.Comprobantes.Commands.EmitirFactura;
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
            idEmpresa: 1, tipo: tipo, puntoVenta: 1, numero: 1,
            fecha: new DateOnly(2026, 6, 3), idCliente: 10,
            razonSocialCliente: "Juan Pérez", cuitCliente: null,
            condicionIvaReceptor: condicion,
            items: items ?? ItemsEjemplo());

    // ─── Cálculo IVA — todas las alícuotas ───────────────────────────────────
    [Theory]
    [InlineData(AlicuotaIva.Porcentaje21,   210.00,  1210.00)]
    [InlineData(AlicuotaIva.Porcentaje10_5, 105.00,  1105.00)]
    [InlineData(AlicuotaIva.Porcentaje5,     50.00,  1050.00)]
    [InlineData(AlicuotaIva.Porcentaje2_5,   25.00,  1025.00)]
    [InlineData(AlicuotaIva.Porcentaje27,   270.00,  1270.00)]
    [InlineData(AlicuotaIva.Porcentaje0,      0.00,  1000.00)]
    [InlineData(AlicuotaIva.Exento,           0.00,  1000.00)]
    public void ItemComprobante_calculo_iva_base1000(
        AlicuotaIva alicuota, double ivaEsperado, double totalEsperado)
    {
        var item = ItemComprobante.Crear(1, "Prod", 1m, 1000m, alicuota);

        item.SubtotalNeto.Should().Be(1000m);
        item.Iva.Should().Be((decimal)ivaEsperado);
        item.Subtotal.Should().Be((decimal)totalEsperado);
    }

    // ─── Redondeo centavo-exacto ──────────────────────────────────────────────
    [Fact]
    public void ItemComprobante_IVA21_cantidad_decimal_no_acumula_drift()
    {
        // 3 unidades × $333.33 = neto $999.99 → IVA 21% = $209.9979 → round $210.00
        var item = ItemComprobante.Crear(1, "Prod", 3m, 333.33m, AlicuotaIva.Porcentaje21);

        (item.SubtotalNeto + item.Iva).Should().Be(item.Subtotal,
            "SubtotalNeto + Iva debe ser igual a Subtotal sin drift de redondeo");
    }

    // ─── Totales con múltiples alícuotas ─────────────────────────────────────
    [Fact]
    public void Comprobante_total_con_cinco_alicuotas_distintas()
    {
        var items = new List<ItemComprobante>
        {
            ItemComprobante.Crear(1, "A", 1m, 1000m, AlicuotaIva.Porcentaje21),   // 1000 + 210
            ItemComprobante.Crear(2, "B", 1m, 1000m, AlicuotaIva.Porcentaje10_5), // 1000 + 105
            ItemComprobante.Crear(3, "C", 1m, 1000m, AlicuotaIva.Porcentaje5),    // 1000 + 50
            ItemComprobante.Crear(4, "D", 1m, 1000m, AlicuotaIva.Porcentaje27),   // 1000 + 270
            ItemComprobante.Crear(5, "E", 1m, 1000m, AlicuotaIva.Exento),         // 1000 + 0
        };

        var comp = ComprobanteEjemplo(items: items);

        comp.TotalNeto.Should().Be(5000m);
        comp.TotalIva.Should().Be(635m);   // 210+105+50+270+0
        comp.Total.Should().Be(5635m);
    }

    // ─── Reglas tipo comprobante vs condición IVA ─────────────────────────────
    [Theory]
    [InlineData(TipoComprobante.FacturaA,    CondicionIva.ConsumidorFinal)]
    [InlineData(TipoComprobante.FacturaA,    CondicionIva.Monotributo)]
    [InlineData(TipoComprobante.FacturaA,    CondicionIva.Exento)]
    [InlineData(TipoComprobante.FacturaA,    CondicionIva.NoInscripto)]
    [InlineData(TipoComprobante.NotaCreditoA, CondicionIva.ConsumidorFinal)]
    [InlineData(TipoComprobante.NotaCreditoA, CondicionIva.Monotributo)]
    [InlineData(TipoComprobante.NotaDebitoA,  CondicionIva.ConsumidorFinal)]
    [InlineData(TipoComprobante.NotaDebitoA,  CondicionIva.Monotributo)]
    public void Comprobante_tipoA_a_condicion_no_inscripto_lanza_DomainException(
        TipoComprobante tipo, CondicionIva condicion)
    {
        var act = () => ComprobanteEjemplo(tipo, condicion);

        act.Should().Throw<DomainException>()
            .WithMessage("*Responsables Inscriptos*");
    }

    [Theory]
    [InlineData(TipoComprobante.FacturaA,    CondicionIva.Inscripto)]
    [InlineData(TipoComprobante.FacturaB,    CondicionIva.ConsumidorFinal)]
    [InlineData(TipoComprobante.FacturaB,    CondicionIva.Monotributo)]
    [InlineData(TipoComprobante.FacturaB,    CondicionIva.Inscripto)]
    [InlineData(TipoComprobante.FacturaC,    CondicionIva.ConsumidorFinal)]
    [InlineData(TipoComprobante.NotaCreditoA, CondicionIva.Inscripto)]
    [InlineData(TipoComprobante.NotaDebitoA,  CondicionIva.Inscripto)]
    public void Comprobante_combinacion_valida_no_lanza(TipoComprobante tipo, CondicionIva condicion)
    {
        var act = () => ComprobanteEjemplo(tipo, condicion);

        act.Should().NotThrow();
    }

    [Fact]
    public void Comprobante_sin_items_lanza_DomainException()
    {
        var act = () => ComprobanteEjemplo(items: []);

        act.Should().Throw<DomainException>().WithMessage("*al menos un ítem*");
    }

    // ─── EsElectronica ────────────────────────────────────────────────────────
    [Theory]
    [InlineData(TipoComprobante.FacturaA,    true)]
    [InlineData(TipoComprobante.FacturaB,    true)]
    [InlineData(TipoComprobante.FacturaC,    true)]
    [InlineData(TipoComprobante.FacturaM,    true)]
    [InlineData(TipoComprobante.NotaCreditoA, true)]
    [InlineData(TipoComprobante.NotaCreditoB, true)]
    [InlineData(TipoComprobante.NotaDebitoA,  true)]
    [InlineData(TipoComprobante.RecibosA,    false)]
    [InlineData(TipoComprobante.RecibosB,    false)]
    public void EsElectronica_retorna_valor_correcto_por_tipo(TipoComprobante tipo, bool esperado)
    {
        // Usamos el método interno del handler via acceso directo al tipo
        var condicion = tipo is TipoComprobante.FacturaA or TipoComprobante.NotaCreditoA or TipoComprobante.NotaDebitoA
            ? CondicionIva.Inscripto
            : CondicionIva.ConsumidorFinal;

        var comp = ComprobanteEjemplo(tipo, condicion);

        // EsElectronica en el dominio usa Cae != null, pero el método del handler
        // lo determina por tipo. Verificamos indirectamente que el tipo esté en el enum.
        comp.Tipo.Should().Be(tipo);

        EmitirFacturaCommandHandler.EsElectronica(tipo).Should().Be(esperado);
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
        comp.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "CaeAsignadoEvent");
    }

    [Fact]
    public void AsignarCae_dos_veces_lanza_DomainException()
    {
        var comp = ComprobanteEjemplo();
        comp.AsignarCae("11111111111111", new DateOnly(2026, 8, 31));

        var act = () => comp.AsignarCae("22222222222222", new DateOnly(2026, 9, 30));

        act.Should().Throw<DomainException>().WithMessage("*ya tiene un CAE*");
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

    // ─── BuildAfipRequest — todas las alícuotas mapeadas ─────────────────────
    [Fact]
    public void BuildAfipRequest_mapea_alicuotas_27_y_5_y_25()
    {
        var items = new List<ItemComprobante>
        {
            ItemComprobante.Crear(1, "A", 1m, 1000m, AlicuotaIva.Porcentaje27),
            ItemComprobante.Crear(2, "B", 1m, 1000m, AlicuotaIva.Porcentaje5),
            ItemComprobante.Crear(3, "C", 1m, 1000m, AlicuotaIva.Porcentaje2_5),
            ItemComprobante.Crear(4, "D", 1m, 500m,  AlicuotaIva.Exento),
        };
        var comp = ComprobanteEjemplo(TipoComprobante.FacturaB, CondicionIva.ConsumidorFinal, items);
        var req = EmitirFacturaCommandHandler.BuildAfipRequest("20111222333", comp, 1, items);

        req.Alicuotas.Should().HaveCount(3, "Exento no va en el array de alícuotas");
        req.Alicuotas.Should().Contain(a => a.IdAfip == (int)AlicuotaIva.Porcentaje27 && a.BaseImponible == 1000m);
        req.Alicuotas.Should().Contain(a => a.IdAfip == (int)AlicuotaIva.Porcentaje5  && a.BaseImponible == 1000m);
        req.Alicuotas.Should().Contain(a => a.IdAfip == (int)AlicuotaIva.Porcentaje2_5 && a.BaseImponible == 1000m);
        req.TotalExento.Should().Be(500m);
    }
}

using System.Text;
using System.Text.Json;
using FluentAssertions;
using NewGest.Domain.Entities.Com;
using NewGest.Domain.Enums;
using NewGest.Infrastructure.Services;

namespace NewGest.IntegrationTests.Facturacion;

public class QrFiscalServiceTests
{
    private readonly QrFiscalService _service = new();

    private static Comprobante ComprobanteConCae()
    {
        var comp = Comprobante.Crear(
            idEmpresa: 1,
            tipo: TipoComprobante.FacturaB,
            puntoVenta: 1,
            numero: 42,
            fecha: new DateOnly(2026, 6, 3),
            idCliente: 10,
            razonSocialCliente: "Test SA",
            cuitCliente: null,
            condicionIvaReceptor: CondicionIva.ConsumidorFinal,
            items: [ItemComprobante.Crear(1, "Prod", 1m, 100m, AlicuotaIva.Porcentaje21)]);
        comp.AsignarCae("12345678901234", new DateOnly(2026, 8, 31));
        return comp;
    }

    [Fact]
    public void GenerarUrl_contiene_dominio_afip()
    {
        var comp = ComprobanteConCae();
        var url = _service.GenerarUrl(comp, "20123456789");

        url.Should().StartWith("https://www.afip.gob.ar/fe/qr/?p=");
    }

    [Fact]
    public void GenerarUrl_parametro_es_base64_json_valido()
    {
        var comp = ComprobanteConCae();
        var url = _service.GenerarUrl(comp, "20123456789");
        var b64 = url.Split("?p=")[1];
        var json = Encoding.UTF8.GetString(Convert.FromBase64String(b64));

        var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("ver").GetInt32().Should().Be(1);
        doc.RootElement.GetProperty("tipoCmp").GetInt32().Should().Be(6); // FacturaB
        doc.RootElement.GetProperty("nroCmp").GetInt64().Should().Be(42);
        doc.RootElement.GetProperty("tipoCodAut").GetString().Should().Be("E");
    }

    [Fact]
    public void GenerarPng_retorna_bytes_no_vacios()
    {
        var comp = ComprobanteConCae();
        var url = _service.GenerarUrl(comp, "20123456789");
        var png = _service.GenerarPng(url);

        png.Should().NotBeEmpty();
        // PNG signature: 89 50 4E 47
        png[0].Should().Be(0x89);
        png[1].Should().Be(0x50);
        png[2].Should().Be(0x4E);
        png[3].Should().Be(0x47);
    }
}

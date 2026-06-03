using System.Text;
using System.Text.Json;
using NewGest.Application.Services;
using NewGest.Domain.Entities.Com;
using QRCoder;

namespace NewGest.Infrastructure.Services;

public class QrFiscalService : IQrFiscalService
{
    public string GenerarUrl(Comprobante comprobante, string cuitEmisor)
    {
        var datos = new
        {
            ver = 1,
            fecha = comprobante.Fecha.ToString("yyyy-MM-dd"),
            cuit = long.Parse(cuitEmisor),
            ptoVta = comprobante.PuntoVenta,
            tipoCmp = (int)comprobante.Tipo,
            nroCmp = comprobante.Numero,
            importe = comprobante.Total,
            moneda = "PES",
            ctz = 1,
            tipoDocRec = comprobante.CuitCliente is not null ? 80 : 99,  // 80=CUIT, 99=consumidor final
            nroDocRec = long.TryParse(comprobante.CuitCliente, out var cuit) ? cuit : 0L,
            tipoCodAut = "E",
            codAut = long.TryParse(comprobante.Cae?.Codigo, out var cae) ? cae : 0L
        };

        var json = JsonSerializer.Serialize(datos);
        var b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        return $"https://www.afip.gob.ar/fe/qr/?p={b64}";
    }

    public byte[] GenerarPng(string url)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.M);
        using var qrCode = new PngByteQRCode(qrData);
        return qrCode.GetGraphic(20);
    }
}

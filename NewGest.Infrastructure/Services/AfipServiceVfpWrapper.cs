using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NewGest.Application.Services;
using NewGest.Domain.Common;
using NewGest.Domain.Enums;

namespace NewGest.Infrastructure.Services;

/// <summary>
/// Implementación temporal que invoca el ejecutable VFP CAEFoxNewgest.exe.
/// Se reemplaza por AfipServiceWsfe en Sprint 12+ sin cambiar consumidores (ver IAfipService).
/// </summary>
public class AfipServiceVfpWrapper : IAfipService
{
    private readonly string _ejecutable;
    private readonly ILogger<AfipServiceVfpWrapper> _logger;

    public AfipServiceVfpWrapper(IConfiguration config, ILogger<AfipServiceVfpWrapper> logger)
    {
        _ejecutable = config["Afip:CaeFoxNewgestPath"]
            ?? throw new InvalidOperationException("Afip:CaeFoxNewgestPath no configurado.");
        _logger = logger;
    }

    public async Task<CaeResponse> SolicitarCaeAsync(ComprobanteAfip comprobante, CancellationToken ct)
    {
        var tempDir = Path.GetTempPath();
        var inputFile = Path.Combine(tempDir, $"afip_req_{Guid.NewGuid():N}.json");
        var outputFile = Path.Combine(tempDir, $"afip_res_{Guid.NewGuid():N}.json");

        try
        {
            var payload = new
            {
                cuitEmisor = comprobante.CuitEmisor,
                tipo = (int)comprobante.Tipo,
                puntoVenta = comprobante.PuntoVenta,
                numeroDesde = comprobante.NumeroDesde,
                numeroHasta = comprobante.NumeroHasta,
                fecha = comprobante.FechaComprobante.ToString("yyyyMMdd"),
                cuitReceptor = comprobante.CuitReceptor,
                totalExento = comprobante.TotalExento,
                totalComprobante = comprobante.TotalComprobante,
                alicuotas = comprobante.Alicuotas.Select(a => new
                {
                    idAfip = a.IdAfip,
                    baseImponible = a.BaseImponible,
                    importe = a.Importe
                }),
                outputFile
            };

            await File.WriteAllTextAsync(inputFile, JsonSerializer.Serialize(payload), ct);

            using var proceso = new System.Diagnostics.Process();
            proceso.StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = _ejecutable,
                Arguments = $"\"{inputFile}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            proceso.Start();
            await proceso.WaitForExitAsync(ct);

            if (proceso.ExitCode != 0)
            {
                var stderr = await proceso.StandardError.ReadToEndAsync(ct);
                _logger.LogError("CAEFoxNewgest exitó con código {Código}: {Error}", proceso.ExitCode, stderr);
                throw new DomainException($"Error al comunicarse con AFIP: {stderr}");
            }

            if (!File.Exists(outputFile))
                throw new DomainException("CAEFoxNewgest no generó archivo de respuesta.");

            var json = await File.ReadAllTextAsync(outputFile, ct);
            var respuesta = JsonSerializer.Deserialize<CaeFoxRespuesta>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? throw new DomainException("Respuesta de AFIP inválida.");

            if (!respuesta.Exito)
                throw new DomainException($"AFIP rechazó el comprobante: {respuesta.Mensaje}");

            return new CaeResponse(
                respuesta.Cae!,
                DateOnly.ParseExact(respuesta.CaeVencimiento!, "yyyyMMdd", null),
                comprobante.NumeroDesde.ToString());
        }
        finally
        {
            if (File.Exists(inputFile)) File.Delete(inputFile);
            if (File.Exists(outputFile)) File.Delete(outputFile);
        }
    }

    public Task<bool> ValidarComprobanteAsync(
        string cuit, TipoComprobante tipo, long numero, CancellationToken ct)
    {
        // Validación no implementada en wrapper VFP — siempre devuelve true
        _logger.LogWarning("ValidarComprobanteAsync no implementado en VFP wrapper.");
        return Task.FromResult(true);
    }

    public Task<PuntoVentaAfip[]> ObtenerPuntosVentaAsync(string cuit, CancellationToken ct)
    {
        _logger.LogWarning("ObtenerPuntosVentaAsync no implementado en VFP wrapper.");
        return Task.FromResult(Array.Empty<PuntoVentaAfip>());
    }

    private record CaeFoxRespuesta(
        bool Exito,
        string? Mensaje,
        string? Cae,
        string? CaeVencimiento
    );
}

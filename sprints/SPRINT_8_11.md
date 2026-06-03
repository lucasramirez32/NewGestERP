# SPRINT 8–11 — Facturación Electrónica y en Blanco
**Semanas:** 33–48 · **Módulos:** M04 (Facturación AFIP) + M05 (Facturación en Blanco)  
**Fase de coexistencia:** B→C (facturación migrada; VFP solo para reportes legacy)  
**Agente principal:** `fullstack-db` · **Agente QA:** `qa`  
**Riesgo:** CRÍTICO — módulo regulatorio, integración con AFIP, numeración fiscal

---

## ⚠️ PRECAUCIONES ANTES DE COMENZAR

1. **Leer COMPLETAMENTE** `FACELEC1.SCX` y `FACELEC2.SCX` antes de escribir una sola línea.
2. **Verificar certificado AFIP:** comprobar `V_Vtocertificado` en VFP. Si vence en < 60 días, renovar primero.
3. **Mover archivos `.p12`** de `C:\newgest\certificado\` a Key Vault (debe estar hecho desde Sprint 0).
4. **Obtener credenciales de homologación AFIP** para ambiente de testing.
5. **Un mes de paralelo mínimo** antes del go-live: mismo período en VFP y .NET, comparar Libro IVA centavo a centavo.

---

## Contexto VFP

| Archivo VFP | Función | LOC |
|---|---|---|
| `FACELEC1.SCX` | Facturación electrónica Tipo A | ~6.000 |
| `FACELEC2.SCX` | Facturación electrónica Tipo B/C | ~6.000 |
| `FACELEC1_LOTE.SCX` | Facturación A en lote | ~3.000 |
| `FACELEC2_LOTE.SCX` | Facturación B/C en lote | ~3.000 |
| `FACELEC1_QR.SCX` | Generación QR fiscal | ~2.000 |
| `FACBLAN1.SCX` / `FACBLAN2.SCX` | Facturación en blanco | ~8.000 |
| `CAEFoxNewgest` | Proceso externo para AFIP WS | Ejecutable VFP |
| `FUNCION.PRG:949–1078` | `FUNCTION Consecutivo()` | Race condition con RLOCK |

---

## Sprint 8–9 — Análisis profundo + Prototipo (Semanas 33–40)

**Sprint 8 es principalmente de análisis y no produce código de producción.**

### S8-1 — Relevamiento funcional FACELEC1.SCX
**Entregable:** `docs/analisis/M04_reglas_negocio.md`

Documentar manualmente (leyendo el .SCX):
- Tipos de comprobante soportados: FC-A, FC-B, FC-C, NC-A, NC-B, NC-C, ND-A, ND-B, ND-C
- Alícuotas de IVA manejadas: 0%, 2.5%, 5%, 10.5%, 21%, 27%
- Condiciones de IVA del cliente → qué tipo de comprobante puede emitir
- Campos obligatorios para AFIP por tipo de comprobante
- Flujo exacto de la comunicación con `CAEFoxNewgest`
- Cómo se genera el QR fiscal (datos del comprobante + CAE)
- Manejo de rechazos AFIP: códigos de error y mensajes al usuario

### S8-2 — Arquitectura IAfipService
**Entregable:** `NewGest.Application/Services/IAfipService.cs` + implementaciones

```csharp
// Interface que permite swap sin cambiar consumidores
public interface IAfipService
{
    Task<CaeResponse> SolicitarCaeAsync(ComprobanteAfip comprobante, CancellationToken ct);
    Task<bool> ValidarComprobanteAsync(string cuit, TipoComprobante tipo, long numero, CancellationToken ct);
    Task<PuntoVenta[]> ObtenerPuntosVentaAsync(string cuit, CancellationToken ct);
}

public record CaeResponse(string CodigoCae, DateOnly FechaVencimiento, string NumeroComprobante);
public record ComprobanteAfip(
    string CuitEmisor,
    TipoComprobante Tipo,
    int PuntoVenta,
    long NumeroDesde,
    long NumeroHasta,
    DateOnly FechaComprobante,
    string? CuitReceptor,
    decimal TotalNeto21,
    decimal IVA21,
    decimal TotalNeto105,
    decimal IVA105,
    decimal TotalExento,
    decimal TotalComprobante
);

// Implementación temporal: wrapper del ejecutable VFP CAEFoxNewgest
public class AfipServiceVfpWrapper : IAfipService
{
    // Invoca CAEFoxNewgest.exe via Process con parámetros en archivo temporal
    // Lee respuesta del archivo de salida que VFP genera
    // Útil mientras se implementa la versión nativa .NET
}

// Implementación final: SDK .NET nativo
public class AfipServiceWsfe : IAfipService
{
    // Usar NuGet AfipDev o implementar el SOAP WS directo
    // Certificado: cargar desde Key Vault o Windows Certificate Store
}
```

---

## Sprint 10–11 — Facturación en producción (Semanas 41–48)

### S10-1 — Entidad Comprobante (Dominio)
```csharp
public class Comprobante : AggregateRoot
{
    public int IdComprobante { get; private set; }
    public int IdEmpresa { get; private set; }
    public TipoComprobante Tipo { get; private set; }
    public int PuntoVenta { get; private set; }
    public long Numero { get; private set; }
    public DateOnly Fecha { get; private set; }
    public int IdCliente { get; private set; }
    public CondicionIva CondicionIvaReceptor { get; private set; }
    public List<ItemComprobante> Items { get; private set; } = [];
    public List<AlicuotaIva> Alicuotas { get; private set; } = [];
    public decimal TotalNeto { get; private set; }
    public decimal TotalIva { get; private set; }
    public decimal Total { get; private set; }
    public CaeInfo? Cae { get; private set; }
    public bool EsElectronica => Cae is not null;
    public int? IdPedidoOrigen { get; private set; }

    public void AsignarCae(CaeResponse cae)
    {
        if (Cae is not null) throw new DomainException("Comprobante ya tiene CAE asignado.");
        Cae = new CaeInfo(cae.CodigoCae, cae.FechaVencimiento);
        // Emitir domain event
        AddDomainEvent(new CaeAsignadoEvent(IdComprobante, cae.CodigoCae));
    }
}
```

### S10-2 — Handler EmitirFactura (CQRS)

Flujo completo de emisión:
1. Obtener próximo número via `NEXT VALUE FOR com.SeqFC_EM000001` (atómico)
2. Calcular IVA por alícuota
3. Crear entidad `Comprobante`
4. Si electrónica: llamar `IAfipService.SolicitarCaeAsync()`
5. Si AFIP rechaza: rollback completo, retornar error descriptivo al usuario
6. Generar PDF (QuestPDF) con datos del comprobante + QR fiscal
7. Registrar en `cnt.LibroIva`
8. Actualizar stock si el comprobante baja mercadería (llama `IStockService`)
9. Persistir en una sola transacción
10. Retornar DTO con número, CAE y URL del PDF

### S10-3 — QR Fiscal (reemplaza FACELEC1_QR.SCX)
```csharp
// Datos del QR según especificación AFIP
public class QrFiscalService
{
    public string GenerarUrl(Comprobante c, string caeCode)
    {
        var datos = new
        {
            ver = 1,
            fecha = c.Fecha.ToString("yyyy-MM-dd"),
            cuit = c.CuitEmisor,
            ptoVta = c.PuntoVenta,
            tipoCmp = (int)c.Tipo,
            nroCmp = c.Numero,
            importe = c.Total,
            moneda = "PES",
            ctz = 1,
            tipoDocRec = c.TipoDocumentoReceptor,
            nroDocRec = c.NumeroDocumentoReceptor,
            tipoCodAut = "E",
            codAut = long.Parse(caeCode)
        };

        var json = JsonSerializer.Serialize(datos);
        var b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        return $"https://www.afip.gob.ar/fe/qr/?p={b64}";
    }
}

// Generación del código QR como imagen para el PDF:
using QRCoder;
var qrGenerator = new QRCodeGenerator();
var qrData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.M);
var qrCode = new PngByteQRCode(qrData);
var png = qrCode.GetGraphic(20);
```

### S10-4 — Facturación en Lote
**VFP fuente:** `FACELEC1_LOTE.SCX` / `FACELEC2_LOTE.SCX`

```csharp
// Procesar hasta 250 comprobantes en una solicitud WSFE (límite AFIP)
public class EmitirFacturasLoteHandler : IRequestHandler<EmitirLoteCommand, LoteResultDto>
{
    // Dividir en chunks de 250 si son más
    // Para cada chunk: SolicitarCaeAsync con rango de números
    // Generar PDFs en paralelo (Task.WhenAll)
    // Reportar resultado por comprobante (éxito / error individual)
}
```

### S10-5 — Facturación en Blanco (M05)
Similar a M04 pero sin comunicación AFIP. Mismo flujo excepto:
- No llama a `IAfipService`
- `Cae` queda en null
- PDF sin QR fiscal (o con leyenda "Sin CAE")

### S10-6 — Pantalla Facturación (HTML + Tailwind + JS)
**Entregable:** `NewGest.Web/src/pages/facturacion/nueva-factura.html` + `js/pages/facturacion/nueva-factura.js`

Reemplaza `FACELEC1.SCX`. Funcionalidades:
- Header: selector de empresa/punto de venta, tipo de comprobante, fecha
- Búsqueda de cliente: input con autocomplete `GET /api/clientes?search=` (dropdown Tailwind)
- Grilla de items editable: búsqueda artículo, cantidad, precio unitario, alícuota IVA — totales recalculados en JS con cada cambio (`input` event)
- Panel derecho sticky: subtotal, IVA por alícuota, total — tipografía grande y clara
- Botón Emitir: se deshabilita mientras espera CAE (`disabled + opacity-50 + cursor-not-allowed`)
- Al recibir respuesta: mostrar CAE + número en un `<dialog>` nativo HTML5 con opción de descargar PDF
- Modo lote: `<input type="file">` + `POST /api/comprobantes/lote` con archivo Excel

---

## Tests — Agente QA

### Unitarios (críticos)
```
✓ Cálculo IVA 21%: base 1000 → IVA 210 → total 1210
✓ Cálculo IVA 10.5%: base 1000 → IVA 105 → total 1105
✓ Cálculo IVA 0% (exento): total = base
✓ Cliente Monotributo → solo puede recibir FC-B
✓ Cliente Responsable Inscripto → puede recibir FC-A, FC-B
✓ QR fiscal: URL correctamente codificada en Base64 (verificar vs app AFIP)
✓ Consecutivo: 1000 llamadas concurrentes → sin duplicados (test de carga)
```

### Integración (ambiente homologación AFIP)
```
✓ FC-B emitida → CAE retornado y válido
✓ FC-A emitida → CAE retornado y válido
✓ NC-B emitida referenciando FC-B → reduce libro IVA correctamente
✓ AFIP rechaza comprobante → error descriptivo, sin número quemado
✓ Lote de 10 comprobantes → todos obtienen CAE
✓ PDF generado con QR: decodificable por app AFIP oficial
✓ PDF: compare visual con el generado por VFP (mismo layout)
```

### Validación paralela (1 mes fiscal mínimo)
```
✓ Libro IVA Ventas: total neto, IVA 21%, IVA 10.5%, total general → diferencia 0 vs VFP
✓ Numeración: ningún número duplicado ni gap entre VFP y .NET
✓ CAEs: todos los CAEs emitidos por .NET son válidos en portal AFIP
✓ PDFs: revisar 50 comprobantes al azar → idénticos al VFP (validación manual)
```

---

## Criterios de aceptación

- [ ] CAE obtenido correctamente para FC-A, FC-B, FC-C en producción
- [ ] Numeración consecutiva sin gaps ni duplicados (test 1000 concurrentes)
- [ ] PDF con QR decodificable por app AFIP oficial
- [ ] Rechazo AFIP manejado con mensaje claro al usuario (sin números "quemados")
- [ ] Libro IVA cuadra centavo a centavo con VFP en 1 mes de paralelo
- [ ] NC y ND reducen/aumentan libro IVA correctamente
- [ ] Facturación en lote: mismo resultado que `*_LOTE.SCX` VFP
- [ ] Stock descontado automáticamente al facturar artículos de inventario
- [ ] Certificado AFIP .p12 cargado desde Key Vault, no desde directorio

---

## Dependencias

- **Requiere:** Sprint 3-4 (Clientes, Artículos) + Sprint 5-7 (Stock)
- **Bloquea:** Sprint 12-13 (Cobranzas imputadas a comprobantes) + Sprint 14-15 (Contabilidad)

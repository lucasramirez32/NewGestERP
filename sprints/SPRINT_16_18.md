# SPRINT 16–18 — Reportes (415 FRX → 40 familias)
**Semanas:** 65–76 · **Módulo:** M15 (Reportes) + M17 (PDF/QR)  
**Fase de coexistencia:** C→D (VFP desactivándose gradualmente)  
**Agente principal:** `fullstack-db` · **Agente QA:** `qa`  
**Riesgo:** CRÍTICO por volumen — 415 archivos .FRX en formato binario propietario VFP

---

## Estrategia de migración de reportes

**Los 415 .FRX NO se migran directamente** (formato binario propietario VFP, no existe migrador confiable completo).

**Enfoque correcto:**
1. Clasificar los 415 reportes en **~40 familias únicas** (muchos son variaciones del mismo)
2. Reescribir las 40 plantillas base
3. Las variaciones se resuelven con **parámetros**, no con archivos separados
4. Prioridad: solo migrar los reportes que los usuarios usan (encuesta previa)

### Clasificación de familias (a relevar con usuarios)

| Familia | Cant aprox | Motor | Prioridad |
|---|---|---|---|
| Comprobantes (FC/NC/ND) | ~30 | QuestPDF | P1 — crítico |
| Recibos de cobro | ~10 | QuestPDF | P1 |
| Libro IVA ventas/compras | ~8 | RDLC / FastReport | P1 — regulatorio |
| Estado de cuenta cliente | ~25 | RDLC | P1 |
| Movimientos de stock | ~35 | Dapper + RDLC | P2 |
| Estadísticas de ventas | ~40 | RDLC / Power BI | P2 |
| Informes contables | ~20 | RDLC | P1 — regulatorio |
| Retenciones/IVA | ~15 | RDLC | P1 — regulatorio |
| Listados maestros | ~50 | ClosedXML (directo XLS) | P2 |
| Viajes/Mutuales | ~20 | RDLC | P3 |
| Comprobantes en lote | ~25 | QuestPDF batch | P2 |
| Utilitarios varios | ~137 | Evaluar caso a caso | P3 |

**FastReport .NET:** tiene importador parcial de .FRX que puede acelerar ~20% de los reportes más simples.

---

## Sprint 16 — Reportes P1 (Semanas 65–68)

### S16-1 — Infraestructura de reportes
**Entregable:** `NewGest.Infrastructure/Reports/`

```csharp
// Interfaz genérica de generación de reportes
public interface IReportService
{
    Task<byte[]> GenerarPdfAsync<T>(string plantilla, T datos, CancellationToken ct);
    Task<byte[]> GenerarExcelAsync<T>(string plantilla, IEnumerable<T> datos, CancellationToken ct);
}

// Implementación con QuestPDF para documentos complejos
public class QuestPdfReportService : IReportService { ... }

// Implementación con ClosedXML para listados simples
public class ClosedXmlReportService : IReportService { ... }
```

### S16-2 — PDF de Comprobante (reemplaza FACELEC1.FRX, etc.)
**Motor:** QuestPDF

Layout del comprobante electrónico:
- Encabezado: logo empresa, datos fiscales, CUIT, condición IVA
- Datos del receptor: razón social, CUIT, domicilio
- Número de comprobante, punto de venta, fecha
- Grilla de items: código, descripción, cantidad, precio unitario, subtotal
- Pie: subtotales por alícuota, IVA, total
- QR fiscal (imagen generada con QRCoder)
- CAE y vencimiento CAE
- Código de barras (opcional)

```csharp
public class ComprobanteDocument : IDocument
{
    private readonly ComprobanteReportDto _data;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(1.5f, Unit.Centimetre);
            page.Content().Column(col =>
            {
                col.Item().Component(new EncabezadoEmpresa(_data.Empresa));
                col.Item().Component(new DatosComprobante(_data));
                col.Item().Component(new GrillaItems(_data.Items));
                col.Item().Component(new TotalesComprobante(_data));
                col.Item().Component(new PieConCae(_data.Cae, _data.QrUrl));
            });
        });
    }
}
```

### S16-3 — Libro IVA (regulatorio P1)
**Motor:** Dapper query + QuestPDF o RDLC

```csharp
// Query Dapper para Libro IVA Ventas
const string SQL_LIBRO_IVA = """
    SELECT
        ROW_NUMBER() OVER (ORDER BY c.Fecha, c.Numero) AS Linea,
        c.Fecha, c.Tipo + '-' + FORMAT(c.PuntoVenta,'0000') + '-' + FORMAT(c.Numero,'00000000') AS Comprobante,
        cl.RazonSocial, cl.CUIT,
        c.TotalNeto21, c.IVA21, c.TotalNeto105, c.IVA105, c.TotalExento, c.Total
    FROM com.Comprobantes c
    INNER JOIN neg.Clientes cl ON cl.IdCliente = c.IdCliente
    WHERE c.IdEmpresa = @idEmpresa
      AND c.Fecha BETWEEN @desde AND @hasta
      AND c.EsVenta = 1
    ORDER BY c.Fecha, c.Numero
    """;
```

---

## Sprint 17 — Reportes P1 restantes + infraestructura QR/PDF (Semanas 69–72)

### S17-1 — Estado de cuenta cliente
- Query Dapper: comprobantes + pagos + saldo acumulado por línea
- Formato similar a `EDO_CTA_FINAL.SCX`

### S17-2 — Recibo de cobro PDF
- Motor: QuestPDF
- Incluye detalle de medios de pago y comprobantes imputados
- Monto en letras (`NumberToLetters.Convert()`)

### S17-3 — Reportes contables
- Balance de sumas y saldos
- Libro Mayor por cuenta
- Motor: RDLC (Microsoft Report Viewer)

### S17-4 — Export a Excel para listados maestros
```csharp
// ClosedXML — reemplaza COPY TO TYPE XLS de VFP
public class ExcelExportService
{
    public byte[] ExportarClientes(IEnumerable<ClienteDto> clientes)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Clientes");

        ws.Cell(1,1).Value = "Código";
        ws.Cell(1,2).Value = "Razón Social";
        ws.Cell(1,3).Value = "CUIT";
        // ...

        int row = 2;
        foreach (var c in clientes)
        {
            ws.Cell(row, 1).Value = c.Codigo;
            ws.Cell(row, 2).Value = c.RazonSocial;
            ws.Cell(row, 3).Value = c.CUIT;
            row++;
        }

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}
```

---

## Sprint 18 — Reportes P2 y migración con FastReport (Semanas 73–76)

### S18-1 — Importar .FRX con FastReport .NET
Para los ~50 reportes de listados maestros más simples, intentar importación automática:

```
1. Instalar FastReport .NET
2. Ejecutar importador en modo batch sobre los 415 .FRX
3. Revisar resultado: ~20% funcionarán directamente, ~30% necesitan ajustes menores
4. Los que no se pueden importar: reescribir manualmente
5. Documentar cuáles fueron importados vs. reescritos
```

### S18-2 — Estadísticas de ventas
- Ventas por período, por cliente, por artículo, por vendedor
- Exportación a Excel (ClosedXML) o visualización en Blazor con gráficos simples
- Para análisis avanzado: exportar datos crudos para Power BI

### S18-3 — Encuesta de uso de reportes
**Antes del Sprint 16:** realizar encuesta con usuarios para identificar cuáles de los 415 reportes usan regularmente. Solo migrar esos. Los demás quedan en modo legacy (VFP) hasta ser requeridos.

---

## Tests — Agente QA

### Validación visual de reportes
```
✓ PDF FC-B: comparar layout con PDF generado por VFP (mismo campo a campo)
✓ PDF FC-B: QR decodificable por app AFIP oficial
✓ Libro IVA: totales idénticos al VFP (diferencia 0)
✓ Estado cuenta cliente: saldo final = saldo VFP
✓ Excel de clientes: abre sin errores en Office 2016+
✓ Performance: Libro IVA de 10.000 registros < 30 segundos
```

### Casos de regresión
```
✓ Reporte que funciona en Chrome también funciona en Edge/Firefox
✓ PDF de 50 páginas no supera 5 MB
✓ Export Excel de 100.000 filas en < 60 segundos
```

---

## Criterios de aceptación

- [ ] Los 40 reportes "estrella" producen salida idéntica a VFP (validación manual)
- [ ] Export a Excel abre sin errores en Office 2016, 2019, 365
- [ ] Libro IVA validado y firmado por contador responsable
- [ ] Performance: reporte de 10.000 registros en < 30 segundos
- [ ] QR fiscal decodificable en todos los PDFs de comprobantes electrónicos
- [ ] Encuesta de uso completada: lista de reportes no usados documentada

---

## Dependencias

- **Requiere:** Sprint 14-15 (Contabilidad — datos para informes contables)
- **Bloquea:** Sprint 19-20 (Integraciones usan la infraestructura de PDF/Email)

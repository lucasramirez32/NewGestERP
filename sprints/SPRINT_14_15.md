# SPRINT 14–15 — Contabilidad
**Semanas:** 57–64 · **Módulos:** M12 (Contabilidad/Libro Mayor)  
**Fase de coexistencia:** C (facturación y cobranzas en .NET)  
**Agente principal:** `fullstack-db` · **Agente QA:** `qa`  
**Riesgo:** CRÍTICO — cierre contable, regulatorio, requiere CPA en el equipo de QA

---

## ⚠️ PRECAUCIÓN

Este módulo requiere un contador/CPA que valide los asientos generados. La diferencia de **un centavo** en el cierre mensual es motivo de bloqueo. No hacer go-live sin validación de contador responsable.

---

## Contexto VFP

| Archivo/Tabla VFP | Función |
|---|---|
| `PLANCTA.DBF` | Plan de cuentas jerárquico |
| `ASIENTOS.DBF` | Asientos contables (manual + automáticos) |
| `LIBROIVA.DBF` | Libro IVA ventas/compras |
| `IMPUTADO.DBF` | Imputación de comprobantes a cuentas |
| `SALDOCTA.DBF` | Saldos de cuenta corriente |
| `EDO_CTA_FINAL.SCX` | Estado de cuenta (versión final) |
| `EDO_CTA_SINSALTO.SCX` | Estado de cuenta sin salto de página |
| `EDO_ANALITICO_*.SCX` | Estado analítico de cuentas |

---

## Sprint 14 — Plan de Cuentas + Asientos (Semanas 57–60)

### S14-1 — Entidades Contables
```csharp
// Plan de cuentas jerárquico
public class CuentaContable : Entity
{
    public int IdCuenta { get; private set; }
    public int IdEmpresa { get; private set; }
    public string Codigo { get; private set; } = default!;   // Ej: "1.1.01"
    public string Descripcion { get; private set; } = default!;
    public int? IdCuentaPadre { get; private set; }
    public NaturalezaCuenta Naturaleza { get; private set; }  // Deudora / Acreedora
    public TipoCuenta Tipo { get; private set; }             // Activo, Pasivo, PN, Resultado
    public bool ImputaDirectamente { get; private set; }     // false = solo agrupadora
    public bool Activa { get; private set; } = true;
}

// Asiento contable (doble partida)
public class Asiento : AggregateRoot
{
    public int IdAsiento { get; private set; }
    public int IdEmpresa { get; private set; }
    public long Numero { get; private set; }              // SEQUENCE cnt.SeqAsientos
    public DateOnly Fecha { get; private set; }
    public string Descripcion { get; private set; } = default!;
    public TipoAsiento TipoAsiento { get; private set; }  // Manual, AutoFactura, AutoPago
    public int? IdComprobanteOrigen { get; private set; }
    public List<PartidaAsiento> Partidas { get; private set; } = [];

    public void Validar()
    {
        // Doble partida: suma débitos = suma créditos
        var totalDebe = Partidas.Sum(p => p.Debe);
        var totalHaber = Partidas.Sum(p => p.Haber);
        if (totalDebe != totalHaber)
            throw new DomainException($"Asiento desequilibrado: Debe={totalDebe} Haber={totalHaber}");
    }
}

public class PartidaAsiento : Entity
{
    public int IdCuenta { get; set; }
    public decimal Debe { get; set; }
    public decimal Haber { get; set; }
    public string? Concepto { get; set; }
}
```

### S14-2 — Asientos automáticos al facturar
Cuando se emite un comprobante (M04), el sistema debe generar el asiento contable automáticamente. Las cuentas a usar dependen del plan de cuentas configurado en `cfg.Parametros`.

```csharp
public class GenerarAsientoFacturaHandler
{
    public async Task Handle(CaeAsignadoEvent @event, CancellationToken ct)
    {
        var comprobante = await _repo.GetAsync(@event.IdComprobante, ct);
        var config = await _config.GetCuentasContablesAsync(comprobante.IdEmpresa, ct);

        var asiento = new Asiento(
            idEmpresa: comprobante.IdEmpresa,
            fecha: comprobante.Fecha,
            descripcion: $"FC {comprobante.Tipo} N° {comprobante.Numero} - {comprobante.NombreCliente}",
            tipo: TipoAsiento.AutoFactura,
            idComprobanteOrigen: comprobante.IdComprobante
        );

        // Débito: Cuentas a cobrar
        asiento.AgregarPartida(config.CuentaCobrar, debe: comprobante.Total, haber: 0);
        // Crédito: Ventas
        asiento.AgregarPartida(config.CuentaVentas, debe: 0, haber: comprobante.TotalNeto);
        // Crédito: IVA Ventas
        if (comprobante.TotalIva > 0)
            asiento.AgregarPartida(config.CuentaIvaVentas, debe: 0, haber: comprobante.TotalIva);

        asiento.Validar(); // lanza si no cuadra
        await _asientoRepo.AddAsync(asiento, ct);
        await _uow.CommitAsync(ct);
    }
}
```

### S14-3 — Pantalla Plan de Cuentas y Asientos (Blazor)
- Árbol jerárquico del plan de cuentas (similar a árbol de Windows)
- ABM de asientos manuales: grilla de partidas, validación automática de cuadre
- Visualización de asientos automáticos generados por facturas/pagos

---

## Sprint 15 — Libro IVA y Saldos (Semanas 61–64)

### S15-1 — Libro IVA
```csharp
// El Libro IVA en .NET se calcula en tiempo real desde com.Comprobantes
// No hay tabla separada (el LIBROIVA.DBF VFP era redundante y fuente de inconsistencias)

public class LibroIvaQuery : IRequest<LibroIvaDto>
{
    public int IdEmpresa { get; set; }
    public int Anio { get; set; }
    public int Mes { get; set; }
    public TipoLibro Tipo { get; set; }  // Ventas, Compras
}

// Handler usa Dapper para la query de performance:
public class LibroIvaHandler : IRequestHandler<LibroIvaQuery, LibroIvaDto>
{
    private const string SQL = """
        SELECT
            c.Fecha, c.Tipo, c.PuntoVenta, c.Numero,
            cl.RazonSocial, cl.CUIT,
            c.TotalNeto, c.TotalIva21, c.TotalIva105, c.Total
        FROM com.Comprobantes c
        INNER JOIN neg.Clientes cl ON cl.IdCliente = c.IdCliente
        WHERE c.IdEmpresa = @IdEmpresa
          AND YEAR(c.Fecha) = @Anio AND MONTH(c.Fecha) = @Mes
          AND c.TipoLibro = @Tipo
        ORDER BY c.Fecha, c.Numero
        """;
}
```

### S15-2 — Estado de cuenta cliente
Reemplaza `EDO_CTA_FINAL.SCX` y variantes. Usa Dapper:

```csharp
// Consulta de cuenta corriente: comprobantes + pagos imputados + saldo
public class EstadoCuentaQuery { ... }
```

---

## Tests — Agente QA (liderados por CPA)

### Unitarios
```
✓ Asiento con debe ≠ haber → excepción
✓ Asiento FC-B $1210: Debe Cuentas a Cobrar $1210, Haber Ventas $1000 + IVA Ventas $210
✓ Asiento pago: Debe Caja $1210, Haber Cuentas a Cobrar $1210
✓ Plan de cuentas: nivel 1 agrupa nivel 2, nivel 2 agrupa nivel 3
```

### Validación paralela (CPA obligatorio)
```
✓ Plan de cuentas .NET = PLANCTA.DBF (count + jerarquía)
✓ Libro IVA Ventas mes X: total neto, IVA 21%, IVA 10.5%, total → diferencia 0 vs VFP
✓ Libro IVA Compras mes X: ídem
✓ Cierre mensual: saldos de todas las cuentas cuadran centavo a centavo
✓ Estado de cuenta cliente: saldo .NET = saldo VFP para 20 clientes seleccionados
```

---

## Criterios de aceptación

- [ ] Plan de cuentas migrado sin pérdida de jerarquía
- [ ] Saldos al inicio del período paralelo = saldos VFP (diferencia exacta 0)
- [ ] Asiento automático de factura = asiento equivalente en VFP
- [ ] Cierre mensual cuadra centavo a centavo (firmado por CPA)
- [ ] Libro IVA validado por contador responsable
- [ ] Estado de cuenta cliente idéntico al VFP

---

## Dependencias

- **Requiere:** Sprint 8-11 (Facturación) + Sprint 12-13 (Cobranzas)
- **Bloquea:** Sprint 16-18 (Reportes necesitan datos contables correctos)

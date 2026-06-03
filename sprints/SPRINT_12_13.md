# SPRINT 12–13 — Cobranzas y Retenciones
**Semanas:** 49–56 · **Módulos:** M06 (Cobranzas/Pagos) + M07 (Retenciones IIBB/IVA)  
**Fase de coexistencia:** C (facturación en .NET; VFP solo reportes)  
**Agente principal:** `fullstack-db` · **Agente QA:** `qa`  
**Riesgo:** Alto — imputación de pagos a comprobantes y retenciones provinciales

---

## Objetivo

Migrar el módulo de cobranzas (recibos, pagos, medios de pago, imputación de comprobantes) y el módulo de retenciones IIBB/IVA. Al final del sprint, el ciclo completo de venta→cobro está en .NET.

---

## Contexto VFP

| Archivo VFP | Función | Notas |
|---|---|---|
| `RECIBOCOBRO.SCX` | Recibo de cobro (imputación de pagos) | Lógica compleja de matching |
| `RECBLAN1.SCX` | Recibo en blanco (sin imputación previa) | |
| `RETENCION_IIBB.SCX` | Retenciones Ingresos Brutos | Reglas por provincia |
| `RETENCION_IVA.SCX` | Retenciones IVA | |
| `FUNCION.PRG:329–417` | `FUNCTION Letter()` | Números a letras — para cheques |
| `PENDIENT.DBF` | Comprobantes pendientes de cobro | Saldo a cobrar por cliente |
| `IMPUTADO.DBF` | Imputación de pagos a comprobantes | Tabla de cruce |

**Antes de codificar:** leer COMPLETAMENTE `RECIBOCOBRO.SCX` — tiene la lógica más compleja del sistema fuera de AFIP.

---

## Sprint 12 — Cobranzas y Pagos (Semanas 49–52)

### S12-1 — Entidades de Cobranzas
```csharp
public class Pago : AggregateRoot
{
    public int IdPago { get; private set; }
    public int IdEmpresa { get; private set; }
    public int IdCliente { get; private set; }
    public long Numero { get; private set; }      // SEQUENCE com.SeqRecibos
    public DateOnly Fecha { get; private set; }
    public decimal Total { get; private set; }
    public List<MedioPago> Medios { get; private set; } = [];
    public List<Imputacion> Imputaciones { get; private set; } = [];
    public string? Observaciones { get; private set; }

    public void ImputarComprobante(int idComprobante, decimal monto)
    {
        // Validar que no supera el saldo pendiente del comprobante
        // Actualizar PendienteSaldo del comprobante
        Imputaciones.Add(new Imputacion(idComprobante, monto));
    }
}

// Reemplaza MEDIOPAG.DBF
public class MedioPago : Entity
{
    public TipoMedioPago Tipo { get; set; }  // Efectivo, Cheque, Transferencia, Tarjeta
    public decimal Monto { get; set; }
    public string? BancoEmisor { get; set; }
    public string? NumeroCheque { get; set; }
    public DateOnly? FechaVencimientoCheque { get; set; }
    public string? NumeroTransferencia { get; set; }
}
```

### S12-2 — Función Letter() en C# (CRÍTICO para cheques)
**VFP fuente:** `FUNCION.PRG:329–417`

```csharp
public static class NumberToLetters
{
    // Migrar exactamente el algoritmo de FUNCTION Letter()
    // CRÍTICO: el texto se imprime en cheques. Un error es un problema legal.
    // Mínimo 50 tests comparando output C# vs output VFP
    
    public static string Convert(decimal amount, string moneda = "PESOS")
    {
        // implementación...
    }
}
```

**Protocolo de validación:** ejecutar `Letter()` en VFP con 50 montos específicos (0.01, 0.99, 1.00, 99.99, 100.00, 999.99, 1000.00, ..., 999999999.99) y comparar resultado exacto con la función C#.

### S12-3 — Imputación de Pagos
La lógica de imputación en VFP es compleja: un pago puede imputarse parcialmente a múltiples comprobantes. Debe quedar saldo 0 o saldo parcial en cada comprobante.

```csharp
public class ImputarPagoHandler : IRequestHandler<ImputarPagoCommand, PagoDto>
{
    public async Task<PagoDto> Handle(ImputarPagoCommand req, CancellationToken ct)
    {
        var comprobantes = await _repo.GetPendientesAsync(req.IdCliente, ct);

        decimal montoRestante = req.TotalPago;
        foreach (var item in req.Imputaciones.OrderBy(i => i.FechaComprobante))
        {
            var comprobante = comprobantes.First(c => c.IdComprobante == item.IdComprobante);
            decimal montoImputar = Math.Min(item.MontoImputar, comprobante.SaldoPendiente);
            
            pago.ImputarComprobante(comprobante.IdComprobante, montoImputar);
            comprobante.ReducirSaldo(montoImputar);
            montoRestante -= montoImputar;
        }

        if (montoRestante > 0)
            pago.GenerarSaldoAFavor(montoRestante);  // Saldo a favor del cliente

        await _unitOfWork.CommitAsync(ct);
        return pago.ToDto();
    }
}
```

### S12-4 — Pantalla Cobros (HTML + Tailwind + JS)
**Entregable:** `NewGest.Web/src/pages/cobranzas/nuevo-recibo.html` + `js/pages/cobranzas/nuevo-recibo.js`

Reemplaza `RECIBOCOBRO.SCX`:
- Selector de cliente → carga automáticamente tabla de comprobantes pendientes (`GET /api/clientes/{id}/pendientes`)
- Tabla de pendientes: checkbox por fila + input de monto imputar (editable, máximo = saldo del comprobante)
- Panel de Medios de Pago: botones "+ Efectivo", "+ Cheque", "+ Transferencia" — cada uno agrega una fila con sus campos específicos
- Total Cobrado vs Total Imputado mostrado en tiempo real — diferencia resaltada en rojo si no cuadra
- Botón Confirmar: deshabilitado hasta que totales cuadren
- Al confirmar: genera PDF del recibo con `Letter()` para monto de cheques — botón de descarga/impresión

---

## Sprint 13 — Retenciones IIBB / IVA (Semanas 53–56)

### S13-1 — Análisis de reglas de retenciones
**Entregable previo:** `docs/analisis/M07_retenciones_reglas.md`

Leer `RETENCION_IIBB.SCX` y `RETENCION_IVA.SCX` completamente y documentar:
- Alícuotas por provincia (varían constantemente — necesitan ser parametrizables)
- Reglas de aplicación según condición del proveedor/cliente
- Tipos de retención: IIBB por provincia, IVA, Ganancias
- Formularios legales generados (certificados de retención)

### S13-2 — Entidad Retención
```csharp
public class Retencion : Entity
{
    public int IdRetencion { get; set; }
    public int IdEmpresa { get; set; }
    public TipoRetencion Tipo { get; set; }  // IIBB, IVA, Ganancias
    public string? Provincia { get; set; }   // Para IIBB
    public decimal AlicuotaPorcentaje { get; set; }
    public decimal BaseImponible { get; set; }
    public decimal MontoRetenido { get; set; }
    public int IdPagoOrigen { get; set; }
    public string NumeroFormulario { get; set; } = default!;
}
```

### S13-3 — Tabla de alícuotas parametrizable
Las alícuotas de IIBB cambian frecuentemente. En VFP están hardcodeadas en el código. En .NET, deben ser configurables sin recompilar:

```sql
CREATE TABLE cfg.AlicuotasRetencion (
    IdAlicuota      INT IDENTITY PRIMARY KEY,
    IdEmpresa       INT NOT NULL,
    TipoRetencion   NVARCHAR(20) NOT NULL,   -- 'IIBB', 'IVA', 'Ganancias'
    Provincia       CHAR(2),                 -- 'BA', 'CF', 'CBA', etc.
    Porcentaje      DECIMAL(5,2) NOT NULL,
    VigenciaDesde   DATE NOT NULL,
    VigenciaHasta   DATE,                    -- NULL = vigente
    CONSTRAINT UQ_Alicuota UNIQUE (IdEmpresa, TipoRetencion, Provincia, VigenciaDesde)
);
```

---

## Tests — Agente QA

### Unitarios
```
✓ Letter(1.00) = "UN PESO" (verificar vs VFP)
✓ Letter(1000.50) = "MIL PESOS CON CINCUENTA CENTAVOS" (verificar vs VFP)
✓ Letter(0.01) = "UN CENTAVO" (verificar vs VFP)
✓ 50 valores comparados con output real de FUNCION.PRG:329–417
✓ Imputación parcial: saldo del comprobante se reduce correctamente
✓ Imputación total: comprobante queda en estado Cobrado
✓ Pago > suma comprobantes: diferencia va a saldo a favor
✓ Retención IIBB BA 1.5%: base 1000 → retención 15.00
```

### Integración
```
✓ POST pago con imputaciones → saldos de comprobantes actualizados en BD
✓ GET /api/clientes/{id}/pendientes → retorna solo comprobantes con saldo > 0
✓ Recibo impreso: monto en letras correcto (verificar vs VFP)
✓ Retención IIBB generada al cobrar → certificado generado con datos correctos
```

### Validación paralela
```
✓ Saldos de cuenta corriente .NET = SALDOCTA.DBF al corte
✓ Total cobrado del mes = suma PAGOS.DBF del mes (diferencia 0)
✓ Retenciones del período = totales en RETENCION_IIBB.DBF (diferencia 0)
```

---

## Criterios de aceptación

- [ ] Imputación de pagos a comprobantes: saldos correctos
- [ ] Pago parcial: saldo pendiente queda correcto en comprobante
- [ ] Recibo impreso con monto en letras: idéntico al VFP (50 casos)
- [ ] Retenciones IIBB/IVA calculadas correctamente por alícuota vigente
- [ ] Certificados de retención generados con formato legal
- [ ] Saldos de cuenta corriente cuadran con VFP en período de paralelo

---

## Dependencias

- **Requiere:** Sprint 8-11 (Facturación — necesita comprobantes para imputar)
- **Bloquea:** Sprint 14-15 (Contabilidad cierra sobre datos de cobros)

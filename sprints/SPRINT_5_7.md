# SPRINT 5–7 — Inventario, Pedidos y Personal
**Semanas:** 21–32 · **Módulos:** M09 (Stock/Inventario) + M11 (Pedidos/Remitos) + M13 (Personal) + M14 (Viajes/Mutuales)  
**Fase de coexistencia:** B (maestros y stock en .NET; facturación aún en VFP)  
**Agente principal:** `fullstack-db` · **Agente QA:** `qa`  
**Riesgo:** Medio — movimientos de stock afectan saldos que luego usa Facturación

---

## Objetivo

Migrar el módulo de inventario (entradas, salidas, transferencias, existencias por depósito), pedidos, remitos, gestión de personal y viajes. Al final del sprint, el stock se gestiona completamente desde .NET.

---

## Contexto VFP

| Archivo VFP | Función | Notas |
|---|---|---|
| `MOVIMIENTOS_STOCK.SCX` | Entradas y salidas de stock | Actualiza ARTEXIS y SALDOST |
| `MOVIMIENTOS_SERIE.SCX` | Control de series/lotes | Trazabilidad por número de serie |
| `FUNCION.PRG:431–472` | `FUNCTION CANTIDADES()` | Calcula existencias — migrar a BD |
| `ARTEXIS.DBF` | Existencias por artículo/depósito | N registros por artículo |
| `SALDOST.DBF` | Saldos totales de stock | 1 registro por artículo |
| `COSTOART.DBF` | Costos históricos | Para valorización de inventario |

**Regla crítica de VFP a conservar:** cuando se registra un movimiento de stock, se actualiza `ARTEXIS` (por depósito) y `SALDOST` (total). En .NET, esta lógica va en el dominio, no en triggers.

---

## Sprint 5 — Stock e Inventario (Semanas 21–24)

### S5-1 — Entidades de Inventario
```csharp
public class MovimientoStock : Entity
{
    public int IdMovimiento { get; private set; }
    public int IdEmpresa { get; private set; }
    public int IdArticulo { get; private set; }
    public int IdDeposito { get; private set; }
    public TipoMovimiento Tipo { get; private set; }  // Entrada, Salida, Transferencia, Ajuste
    public decimal Cantidad { get; private set; }
    public decimal CostoUnitario { get; private set; }
    public string? NumeroSerie { get; private set; }
    public int? IdComprobanteOrigen { get; private set; }  // FK a com.Comprobantes cuando aplique
    public DateTime FechaMovimiento { get; private set; }
}

// Reemplaza ARTEXIS: existencia por artículo + depósito
public class ExistenciaDeposito : Entity
{
    public int IdArticulo { get; set; }
    public int IdDeposito { get; set; }
    public decimal Cantidad { get; set; }
    public decimal CostoPromedio { get; set; }  // método PEPS/PPP según configuración
}
```

### S5-2 — Lógica de movimientos (reemplaza FUNCTION CANTIDADES)
```csharp
// En lugar de recalcular desde cero (VFP), mantener saldos actualizados en cada movimiento
public class StockService : IStockService
{
    public async Task RegistrarMovimientoAsync(MovimientoStock mov, CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            await _db.MovimientosStock.AddAsync(mov, ct);

            // Actualizar existencia por depósito (reemplaza UPDATE en ARTEXIS)
            var existencia = await _db.ExistenciasDeposito
                .FirstOrDefaultAsync(e => e.IdArticulo == mov.IdArticulo && e.IdDeposito == mov.IdDeposito, ct);

            if (existencia is null)
            {
                existencia = new ExistenciaDeposito { IdArticulo = mov.IdArticulo, IdDeposito = mov.IdDeposito };
                _db.ExistenciasDeposito.Add(existencia);
            }

            existencia.Cantidad += mov.Tipo == TipoMovimiento.Salida ? -mov.Cantidad : mov.Cantidad;

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }
}
```

### S5-3 — API Stock
```
GET  /api/stock/{idArticulo}/existencias          → existencias por depósito
GET  /api/stock/{idArticulo}/movimientos          → historial de movimientos
POST /api/stock/movimientos                       → registrar entrada/salida/ajuste
GET  /api/stock/alertas-reposicion               → artículos bajo stock mínimo
GET  /api/stock/valorizado                        → inventario valorizado (Dapper query)
```

### S5-4 — Pantalla Stock (HTML + Tailwind + JS)
**Entregable:** `NewGest.Web/src/pages/stock/movimientos.html` + `js/pages/stock/movimientos.js`

- Tabla de existencias por artículo/depósito con badges de color (verde/amarillo/rojo según nivel)
- Formulario de movimiento: tipo (Entrada/Salida/Ajuste), artículo (autocomplete), depósito, cantidad
- Historial de movimientos con filtros de fecha y artículo — usa `GET /api/stock/{id}/movimientos`
- Alerta visual Tailwind (`bg-red-50 border-l-4 border-red-500`) cuando stock < stock mínimo

---

## Sprint 6 — Pedidos y Remitos (Semanas 25–28)

### S6-1 — Entidad Pedido
```csharp
public class Pedido : AggregateRoot
{
    public int IdPedido { get; private set; }
    public int IdCliente { get; private set; }
    public int IdVendedor { get; private set; }
    public EstadoPedido Estado { get; private set; }  // Pendiente, Parcial, Entregado, Anulado
    public List<ItemPedido> Items { get; private set; } = [];
    public DateTime FechaPedido { get; private set; }
    public DateTime? FechaEntregaEstimada { get; private set; }
    public string? Observaciones { get; private set; }

    public Remito GenerarRemito(IEnumerable<ItemRemito> itemsADespachar)
    {
        // Validar que los artículos están en el pedido
        // Actualizar estado del pedido (Parcial si quedan items)
        // Retornar remito para persistir
        return Remito.Crear(this, itemsADespachar);
    }
}
```

### S6-2 — Flujo Pedido → Remito
- Pedido se crea en estado Pendiente
- Al generar Remito: se descuenta stock (llama a `IStockService.RegistrarMovimientoAsync`)
- Si todos los items entregados → Pedido pasa a Entregado
- Remito puede luego originar Factura (Sprint 8)

---

## Sprint 7 — Personal, Viajes y Mutuales (Semanas 29–32)

### S7-1 — Módulo Personal (M13)
**VFP fuente:** `PERSONAL.DBF`, `HISPER.DBF`

```csharp
public class Empleado : Entity
{
    public int IdEmpleado { get; private set; }
    public int IdEmpresa { get; private set; }
    public string Legajo { get; private set; } = default!;
    public string ApellidoNombre { get; private set; } = default!;
    public string? CUIL { get; private set; }
    public RolEmpleado Rol { get; private set; }  // Vendedor, Administrativo, etc.
    public bool EsVendedor { get; private set; }
    public decimal? ComisionPorcentaje { get; private set; }
}
```

### S7-2 — Módulo Viajes y Mutuales (M14)
**VFP fuente:** `VIAJES.SCX`, `MUTUALES.DBF`, `REMVIAJE.DBF`

- Gestión de viajes (entidades `Viaje`, `RemitosViaje`)
- Mutuales: maestro + asociación a clientes/empleados
- Liquidación de mutuales → genera comprobante de pago (posponer integración a Sprint 12)

---

## Tests — Agente QA

### Unitarios
```
✓ MovimientoStock salida: existencia disminuye correctamente
✓ MovimientoStock entrada: existencia aumenta correctamente
✓ Ajuste a cero: existencia queda en 0
✓ Existencia nunca negativa (configuración: bloquear o permitir, según parámetro)
✓ CuilValidator: 50 CUILs verificados vs FUNCION.PRG:625–664
✓ Pedido.GenerarRemito con items parciales: estado queda en Parcial
```

### Integración
```
✓ POST movimiento stock → existencia actualizada en BD
✓ Concurrencia: 10 movimientos simultáneos sobre mismo artículo → saldo correcto
✓ Remito generado desde pedido → stock descontado
✓ GET existencias responde en < 200ms para catálogo de 10.000 artículos
```

### Validación paralela
```
✓ Saldos de stock en .NET = SALDOST.DBF al momento del corte
✓ Existencias por depósito = ARTEXIS.DBF al momento del corte
✓ Verificar con 20 artículos representativos durante 2 semanas de operación paralela
```

---

## Criterios de aceptación

- [ ] Movimientos de stock registrados en .NET actualizan existencias correctamente
- [ ] Saldos al inicio del paralelo = saldos VFP (diferencia 0)
- [ ] Concurrencia: 10 movimientos simultáneos sin inconsistencias
- [ ] Pedidos crean remitos que descuentan stock
- [ ] ABM de empleados/vendedores funcional
- [ ] Módulo Viajes/Mutuales: CRUD completo

---

## Dependencias

- **Requiere:** Sprint 3-4 (Artículos migrados)
- **Bloquea:** Sprint 8 (Facturación necesita stock disponible en tiempo real)

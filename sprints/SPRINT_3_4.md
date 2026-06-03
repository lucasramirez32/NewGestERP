# SPRINT 3–4 — Catálogo y Maestros
**Semanas:** 13–20 · **Módulos:** M08 (Clientes) + M10 (Artículos) + M03 (Menú)  
**Fase de coexistencia:** A→B (primeros módulos .NET en producción)  
**Agente principal:** `fullstack-db` · **Agente QA:** `qa`  
**Riesgo:** Bajo-Medio — sin lógica fiscal; es el primer módulo que usuarios reales usan

---

## Objetivo

Migrar los maestros de Clientes y Artículos con sus ABM completos, validación de CUIT/CUIL en C#, y el menú de navegación de la app. Al final de este sprint, los usuarios pueden gestionar el catálogo desde la app .NET mientras VFP sigue en producción.

---

## Contexto VFP (leer antes de codificar)

| Archivo VFP | Propósito | Notas |
|---|---|---|
| `CONSULTE.SCX` | ABM de Clientes | Validación CUIT en tiempo real |
| `CONSULA.SCX` | ABM de Artículos | Precios con lista y descuentos |
| `AGRUPACION_ARTICULOS.SCX` | Grupos de artículos | Árbol jerárquico |
| `FUNCION.PRG:595–623` | `FUNCTION Cuit()` | Algoritmo módulo 11 — migrar exacto |
| `FUNCION.PRG:625–664` | `FUNCTION Cuil()` | Similar a CUIT |
| `EDO_CTA*.SCX` | Estado de cuenta cliente | Depende de M06/M12 — posponer |
| `ANULACIONCTE.SCX` | Anulación de cliente | Lógica de dependencias |

---

## Sprint 3 — Módulo Clientes (Semanas 13–16)

### S3-1 — Entidad Cliente (Dominio)
```csharp
public class Cliente : Entity
{
    public int IdCliente { get; private set; }
    public int IdEmpresa { get; private set; }
    public string Codigo { get; private set; } = default!;
    public string RazonSocial { get; private set; } = default!;
    public string? CUIT { get; private set; }
    public CondicionIva CondicionIva { get; private set; }
    public string? Domicilio { get; private set; }
    public string? Localidad { get; private set; }
    public int? IdZona { get; private set; }
    public bool Activo { get; private set; } = true;

    public static Cliente Crear(int idEmpresa, CrearClienteDto dto)
    {
        if (!string.IsNullOrEmpty(dto.CUIT) && !CuitValidator.EsValido(dto.CUIT))
            throw new DomainException($"CUIT inválido: {dto.CUIT}");
        // ...
    }
}
```

### S3-2 — API Clientes (CQRS)
**Endpoints:**
```
GET    /api/clientes?search=&zona=&condicionIva=&page=&pageSize=
GET    /api/clientes/{id}
POST   /api/clientes
PUT    /api/clientes/{id}
DELETE /api/clientes/{id}   (soft delete → Activo = false)
GET    /api/clientes/{id}/saldo-pendiente   (para M06 — retorna vacío hasta Sprint 12)
```

### S3-3 — Pantalla Clientes (HTML + Tailwind + JS)
**Entregable:** `NewGest.Web/src/pages/clientes.html` + `js/pages/clientes.js`

Reemplaza `CONSULTE.SCX`. Funcionalidades:
- Tabla con clases Tailwind: `divide-y`, `hover:bg-gray-50`, badges de condición IVA
- Búsqueda con debounce (300ms) mientras escribe — llama a `GET /api/clientes?search=`
- Validación CUIT en tiempo real usando `validarCuit()` de `js/utils.js` (mismo algoritmo que VFP)
- Modal ABM con Web Component `<ng-modal>`
- Import masivo: `<input type="file" accept=".xlsx">` + endpoint `POST /api/clientes/import`

### S3-4 — Algoritmo CUIT/CUIL en C# (con tests)
**Entregable:** `NewGest.Domain/Services/CuitValidator.cs`

```csharp
public static class CuitValidator
{
    private static readonly int[] Multiplicadores = [5, 4, 3, 2, 7, 6, 5, 4, 3, 2];

    public static bool EsValido(string? cuit)
    {
        if (string.IsNullOrWhiteSpace(cuit)) return false;
        cuit = cuit.Replace("-", "").Replace(" ", "");
        if (cuit.Length != 11 || !cuit.All(char.IsDigit)) return false;
        if (!new[] {"20","23","24","27","30","33","34"}.Contains(cuit[..2])) return false;

        int suma = Multiplicadores.Select((m, i) => m * (cuit[i] - '0')).Sum();
        int resto = suma % 11;
        int digitoVerif = resto switch { 0 => 0, 1 => 9, _ => 11 - resto };
        return digitoVerif == (cuit[10] - '0');
    }
}
```

**Test mínimo requerido (QA): 50 CUITs reales verificados contra el resultado de `FUNCTION Cuit()` en VFP.**

---

## Sprint 4 — Módulo Artículos + Menú (Semanas 17–20)

### S4-1 — Entidad Artículo (Dominio)
```csharp
public class Articulo : Entity
{
    public int IdArticulo { get; private set; }
    public int IdEmpresa { get; private set; }
    public string Codigo { get; private set; } = default!;
    public string Descripcion { get; private set; } = default!;
    public int IdGrupo { get; private set; }
    public int IdUnidad { get; private set; }
    public decimal PrecioLista { get; private set; }
    public decimal PrecioCosto { get; private set; }
    public decimal PorcentajeIva { get; private set; }
    public bool Activo { get; private set; } = true;
    public string? Observaciones { get; private set; }  // campo Memo VFP
}
```

### S4-2 — API Artículos
```
GET    /api/articulos?search=&grupo=&page=&pageSize=
GET    /api/articulos/{id}
POST   /api/articulos
PUT    /api/articulos/{id}
DELETE /api/articulos/{id}
GET    /api/articulos/{id}/existencias   (stock disponible — requiere Sprint 5)
GET    /api/grupos-articulos             (árbol de grupos)
POST   /api/grupos-articulos
```

### S4-3 — Menú principal (M03)
**Entregable:** `NewGest.Web/src/js/components/ng-sidebar.js` (Web Component)

Reemplaza `MENU.SCX` y `MENU2.SCX`. Barra lateral con Tailwind (`fixed left-0 top-0 h-full w-64 bg-gray-900`). El menú es dinámico: al cargar, llama a `GET /api/auth/permisos` y renderiza solo las secciones para las que el usuario tiene acceso. Estructura de íconos + texto idéntica al layout de VFP para reducir fricción de adopción.

### S4-4 — Dashboard principal
**Entregable:** `NewGest.Web/src/pages/index.html` + `js/pages/dashboard.js`

Reemplaza `PRESENTA.SCX` y `ENTRADA.SCX`. Mostrar:
- Ventas del día / semana
- Comprobantes pendientes de cobro
- Alertas: certificado AFIP próximo a vencer (`V_Vtocertificado`)
- Accesos directos a los módulos según permisos

---

## Tests — Agente QA

### Unitarios
```
✓ CuitValidator: 50 CUITs válidos e inválidos (verificar vs FUNCION.PRG:595–623)
✓ CuilValidator: 50 CUILs válidos e inválidos (verificar vs FUNCION.PRG:625–664)
✓ Cliente.Crear con CUIT inválido → DomainException
✓ Cliente.Crear con CUIT vacío → OK (algunos clientes no tienen CUIT)
✓ Artículo con precio negativo → DomainException
```

### Integración
```
✓ GET /api/clientes → lista paginada, filtros funcionan
✓ POST /api/clientes con CUIT inválido → 422
✓ POST /api/clientes con CUIT duplicado (mismo idEmpresa) → 409
✓ DELETE /api/clientes/{id} → soft delete (Activo=false), no elimina físicamente
✓ Import Excel: 100 clientes importados sin errores
✓ Grilla Clientes: renderiza en Blazor sin errores de consola
```

### Validación paralela (VFP ↔ .NET)
```
✓ Count de clientes migrados = count en CLIENTES.DBF
✓ Count de artículos migrados = count en STOCK.DBF
✓ Buscar por CUIT: mismos resultados en VFP y .NET
```

---

## Criterios de aceptación

- [ ] ABM de Clientes funcional en .NET con validación CUIT
- [ ] ABM de Artículos funcional en .NET con grupos jerárquicos
- [ ] Validación CUIT/CUIL: mismos resultados que `FUNCION.PRG` VFP (50 tests)
- [ ] Menú dinámico según permisos del usuario
- [ ] Count de registros migrados coincide con DBF originales
- [ ] Búsqueda por nombre/CUIT/código: resultados idénticos a VFP

---

## Dependencias

- **Requiere:** Sprint 0 (datos migrados) + Sprint 1-2 (autenticación)
- **Bloquea:** Sprint 5 (Stock usa Artículos), Sprint 8 (Facturación usa Clientes y Artículos)

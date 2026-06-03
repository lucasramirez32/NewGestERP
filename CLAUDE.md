# NEWGEST ERP — Migración VFP 9 → .NET 8

## Contexto del proyecto

**Sistema:** NewGest — ERP administrativo-contable para empresas argentinas  
**Origen:** Visual FoxPro 9.0 (file-server, ~120.000 LOC, 3.261 archivos, 2,7 GB)  
**Destino:** ASP.NET Core 8 + HTML/Tailwind/JS + SQL Server 2022  
**Alcance:** 20 módulos, 415 reportes, integración AFIP (facturación electrónica CAE)  
**Duración:** 24–30 meses · ~625 días-dev · equipo estimado: 3 dev .NET + 1 DBA + 1 QA  
**Documento base:** `auditoria_migracion_VFP_dotNET.md`

---

## Stack tecnológico (decisión tomada)

| Capa | Tecnología | Notas |
|---|---|---|
| Frontend | HTML Vanilla + Tailwind CSS v3 + JavaScript ES2022 | Sin frameworks JS. Vite como bundler/dev server |
| API | ASP.NET Core 8 Web API (REST) | CQRS + MediatR + Repository |
| ORM transaccional | EF Core 8 — **Code First** | Entidades C# → migrations → BD. Nunca DDL manual |
| ORM reportes | Dapper | Queries complejas, los 415 reportes |
| Base de datos | SQL Server 2022 | Schemas: neg / com / cnt / inv / cfg / aud |
| Autenticación | ASP.NET Core Identity + JWT | BCrypt, multi-empresa via Claims. JWT en localStorage |
| PDF | QuestPDF | Reemplaza Ghostscript |
| Excel | ClosedXML | Reemplaza COM Excel |
| Email | MailKit | Reemplaza CsFoxySmtp.dll |
| QR | QRCoder (NuGet) | Reemplaza FoxBarcodeQR.pjx |
| AFIP WS | `AfipDev` NuGet + wrapper VFP temporal | Ver interfaz IAfipService |

---

## Estructura de solución (.NET)

```
NewGest.sln
├── NewGest.Domain/          # Entidades, enums, interfaces de dominio
├── NewGest.Application/     # CQRS Commands/Queries, DTOs, IServices
├── NewGest.Infrastructure/  # EF Core, Dapper, repos, servicios externos
└── NewGest.Api/             # ASP.NET Core Web API (también sirve wwwroot/)

NewGest.Web/                 # Frontend: HTML Vanilla + Tailwind CSS + JS (proyecto Node)
├── src/
│   ├── pages/               # Un .html por módulo
│   ├── js/
│   │   ├── api/             # Fetch wrappers por recurso
│   │   ├── components/      # Web Components reutilizables
│   │   ├── auth.js          # JWT: login, logout, refresh, interceptor
│   │   └── utils.js         # CUIT validator, formateo moneda/fechas
│   └── css/input.css        # @tailwind directives
├── dist/                    # Output Vite build → se copia a Api/wwwroot/
├── tailwind.config.js
├── vite.config.js           # Dev proxy: /api → localhost:5000
└── package.json             # devDeps: tailwindcss, vite, postcss, autoprefixer

tests/
├── NewGest.UnitTests/       # xUnit + FluentAssertions + Moq
├── NewGest.IntegrationTests/# TestContainers (SQL Server Docker)
└── NewGest.UatTests/        # Playwright UI + datos reales anonimizados

tools/
├── etl/                     # Python: DBF → SQL Server (dbfread)
└── sync/                    # Servicio Windows: sincronización VFP↔SQL
```

---

## Schemas SQL Server

```sql
neg  -- Negocio: clientes, artículos, proveedores, zonas
com  -- Comercial: comprobantes (EMICOMPR), pagos, imputaciones
cnt  -- Contabilidad: plan de cuentas (PLANCTA), asientos, libro IVA
inv  -- Inventario: stock, movimientos, existencias por depósito
cfg  -- Configuración: empresas (reemplaza directorios EM######), parámetros
aud  -- Auditoría: log de acciones (reemplaza AUDITORIA.DBF)
```

---

## Reglas de desarrollo obligatorias

1. **Sin COM Automation nunca.** Solo ClosedXML para Excel, QuestPDF para PDF.
2. **Contraseñas:** BCrypt exclusivamente. Las contraseñas VFP (XOR) no se migran: forzar reset en primer login.
3. **IDCTRLTRAN reemplazado por SEQUENCE SQL.** Nunca simular el RLOCK() de VFP.
4. **IAfipService:** todo acceso a AFIP via esta interfaz. La implementación VFP wrapper es temporal (Sprints 8–11); luego se cambia a `AfipServiceWsfe` sin tocar consumidores.
5. **Campos memo (tipo M en VFP):** migrar como `NVARCHAR(MAX)`, codificación cp1252 → UTF-16 en ETL.
6. **Fechas vacías VFP** (`CTOD('  /  /    ')`) → `NULL` en SQL, nunca `DateTime.MinValue`.
7. **Datos de prueba:** usar siempre la base `NewGest_Test` con datos anonimizados. Jamás conectar tests a producción.
8. **Auditoría:** toda escritura en com.*, cnt.* y cfg.* dispara trigger de auditoría en `aud.EventLog`.
9. **Multi-empresa:** el `IdEmpresa` viaja en JWT Claims. Todos los repositorios filtran por él.
10. **Reportes:** los 415 .FRX se clasifican en ~40 familias. Nunca crear un archivo por variante; usar parámetros.

---

## Módulos críticos (no tocar sin leer el código VFP fuente)

| Módulo | Archivo VFP fuente | Por qué crítico |
|---|---|---|
| M04 Facturación AFIP | FACELEC1.SCX, FACELEC2.SCX | CAE + firma digital + QR fiscal |
| M12 Contabilidad | ASIENTOS, PLANCTA, LIBROIVA | Cierre contable centavo a centavo |
| M15 Reportes | 415 archivos .FRX | Formato binario propietario VFP |
| Función Consecutivo() | FUNCION.PRG:949–1078 | Numeración sin duplicados |
| Función CUIT/CUIL | FUNCION.PRG:595–664 | Algoritmo módulo 11, regulatorio |
| Función Letter() | FUNCION.PRG:329–417 | Números a letras para cheques |

---

## Agentes disponibles

Usar con `/agent` en Claude Code o como system prompt en Antigravity 2.0:

- **`.claude/agents/fullstack-db.md`** — Desarrollo .NET + migraciones SQL + ETL  
- **`.claude/agents/qa.md`** — Testing, criterios de aceptación, validación paralela VFP↔.NET

---

## Sprints

Ver directorio `sprints/` para el detalle completo de cada sprint:

```
sprints/
├── SPRINT_0.md          Semanas 1–4:   SQL Server + ETL + Auditoría
├── SPRINT_1_2.md        Semanas 5–12:  Autenticación y Parámetros
├── SPRINT_3_4.md        Semanas 13–20: Clientes, Artículos, Maestros
├── SPRINT_5_7.md        Semanas 21–32: Inventario, Pedidos, Personal
├── SPRINT_8_11.md       Semanas 33–48: Facturación (módulo más crítico)
├── SPRINT_12_13.md      Semanas 49–56: Cobranzas y Retenciones
├── SPRINT_14_15.md      Semanas 57–64: Contabilidad
├── SPRINT_16_18.md      Semanas 65–76: Reportes (415 FRX → 40 familias)
└── SPRINT_19_20.md      Semanas 77–84: Email, PDF, QR, Exportaciones + UAT final
```

---

## Fases de coexistencia VFP + .NET

```
Fase A (Sprint 0–4):   Solo datos en SQL Server. VFP sigue siendo productivo.
Fase B (Sprint 5–11):  Maestros y stock en .NET. VFP consume datos SQL.
Fase C (Sprint 12–15): Facturación y contabilidad en .NET. VFP solo reportes.
Fase D (Sprint 16–20): Reportes migrados. VFP desactivado. Go-live completo.
```

---

## Convenciones Git

**Repositorio:** `https://github.com/lucasramirez32/NewGestERP.git`

### Ramas

```
main          → código en producción (protegida, solo merge via PR aprobado)
develop       → integración continua del sprint activo
sprint/S0     → rama de trabajo por sprint  (sprint/S0, sprint/S1-2, sprint/S3-4, ...)
feature/<id>  → tarea individual dentro de un sprint  (feature/S0-1-dbcontext)
hotfix/<desc> → corrección urgente sobre main
```

### Flujo de trabajo

```
1. Crear rama desde develop:  git checkout -b feature/S0-1-dbcontext develop
2. Commits libremente durante el desarrollo (sin aprobación del usuario)
3. Al completar la tarea: PR de feature/* → sprint/*
4. Al completar el sprint: PR de sprint/* → develop
5. Go-live de módulo: PR de develop → main  (requiere aprobación explícita)
6. git push SIEMPRE requiere aprobación explícita del usuario
```

### Formato de commits (Conventional Commits)

```
<tipo>(<alcance>): <descripción en español>

feat(M02):     nueva funcionalidad
fix(M04):      corrección de bug
refactor(M12): mejora sin cambio de comportamiento
test(M02):     agrega o modifica tests
docs:          solo documentación
chore(S0):     migraciones EF Core, deps, config
db(S0):        cambios de schema (migrations)

Ejemplos:
  feat(M02): agregar autenticación BCrypt con ASP.NET Identity
  db(S0): migration InitialCreate — schemas + entidades base
  test(M04): 50 casos CUIT/CUIL verificados contra VFP
  fix(M04): corregir cálculo IVA para clientes exentos
```

### Política de push y contexto

- **`git push` bloqueado** por defecto en `.claude/settings.json` — requiere aprobación explícita del usuario antes de ejecutarse.
- **`git commit` permitido** sin aprobación — el agente puede commitear libremente durante el desarrollo.
- **Después de cada push aprobado:** el hook `.claude/hooks/post-bash.ps1` registra automáticamente la rama, commits y timestamp en `memory/push-log.md`.
- El agente también actualiza `memory/project_newgest_migration.md` para reflejar el avance del sprint.

### Archivos que NUNCA van al repo

Ver `.gitignore`. Especialmente: `*.p12`, `*.pfx`, `appsettings.Production.json`, `.env`, secretos AFIP.

---

## Riesgos prioritarios

| ID | Riesgo | Mitigación |
|---|---|---|
| R01 | Cambios en WS AFIP | Mantener wrapper VFP como fallback |
| R05 | Race condition en IDCTRLTRAN | SEQUENCE SQL desde Sprint 0 |
| R06 | Certificados AFIP vencidos | Monitorear antes del Sprint 8 |
| R08 | Archivos .p12 expuestos | Mover a Key Vault ANTES del Sprint 0 |
| R07 | Equipo sin conocimiento contable argentino | CPA en equipo QA |

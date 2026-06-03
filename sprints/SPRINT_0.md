# SPRINT 0 — Fundación de datos
**Semanas:** 1–4 · **Fase de coexistencia:** A (VFP sigue productivo)  
**Agente principal:** `fullstack-db` · **Agente QA:** `qa`  
**Riesgo:** Medio — sin cambios visibles al usuario, pero base de todo lo demás

---

## Objetivo

Poner SQL Server 2022 operativo con **todos los datos migrados** desde los DBF de VFP, sin cambiar el sistema en producción. El ETL debe ser repetible (idempotente) para poder re-ejecutarse antes del cutover.

---

## Tareas

### S0-1 — Setup SQL Server + Primera migración EF Core (Code First)
**Responsable:** DBA + fullstack-db  
**Entregable:** `NewGest.Infrastructure/Data/NewgestDbContext.cs` + migration `InitialCreate`

**El esquema de base de datos se genera 100% desde C#.** No se escriben scripts DDL. El orden es:

```
1. Definir entidades en NewGest.Domain
2. Configurarlas con IEntityTypeConfiguration en NewGest.Infrastructure
3. Registrar DbSets en NewgestDbContext
4. dotnet ef migrations add InitialCreate --project NewGest.Infrastructure --startup-project NewGest.Api
5. dotnet ef database update
```

#### DbContext

```csharp
// NewGest.Infrastructure/Data/NewgestDbContext.cs
public class NewgestDbContext : DbContext
{
    public NewgestDbContext(DbContextOptions<NewgestDbContext> options) : base(options) { }

    // cfg
    public DbSet<Empresa> Empresas => Set<Empresa>();
    public DbSet<Parametro> Parametros => Set<Parametro>();

    // neg
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Articulo> Articulos => Set<Articulo>();
    public DbSet<GrupoArticulo> GruposArticulos => Set<GrupoArticulo>();
    public DbSet<Unidad> Unidades => Set<Unidad>();
    public DbSet<Zona> Zonas => Set<Zona>();
    public DbSet<Empleado> Empleados => Set<Empleado>();

    // com
    public DbSet<Comprobante> Comprobantes => Set<Comprobante>();
    public DbSet<ItemComprobante> ItemsComprobante => Set<ItemComprobante>();
    public DbSet<Pago> Pagos => Set<Pago>();
    public DbSet<MedioPago> MediosPago => Set<MedioPago>();
    public DbSet<Imputacion> Imputaciones => Set<Imputacion>();

    // inv
    public DbSet<MovimientoStock> MovimientosStock => Set<MovimientoStock>();
    public DbSet<ExistenciaDeposito> ExistenciasDeposito => Set<ExistenciaDeposito>();

    // cnt
    public DbSet<CuentaContable> CuentasContables => Set<CuentaContable>();
    public DbSet<Asiento> Asientos => Set<Asiento>();
    public DbSet<PartidaAsiento> PartidasAsiento => Set<PartidaAsiento>();

    // aud
    public DbSet<EventLog> EventLogs => Set<EventLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Crear los 6 schemas SQL Server
        modelBuilder.HasDefaultSchema("dbo"); // fallback
        
        // Aplicar todas las configuraciones del assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NewgestDbContext).Assembly);
    }
}
```

#### Configuración de entidad (Fluent API — ejemplo Empresa)

```csharp
// NewGest.Infrastructure/Data/Configurations/EmpresaConfiguration.cs
public class EmpresaConfiguration : IEntityTypeConfiguration<Empresa>
{
    public void Configure(EntityTypeBuilder<Empresa> builder)
    {
        builder.ToTable("Empresas", schema: "cfg");

        builder.HasKey(e => e.IdEmpresa);
        builder.Property(e => e.Codigo).IsRequired().HasColumnType("CHAR(6)");
        builder.Property(e => e.RazonSocial).IsRequired().HasMaxLength(100);
        builder.Property(e => e.CUIT).IsRequired().HasColumnType("CHAR(13)");
        builder.Property(e => e.CondicionIVA).IsRequired();
        builder.Property(e => e.Activa).IsRequired().HasDefaultValue(true);
        builder.Property(e => e.FechaAlta).IsRequired().HasDefaultValueSql("GETDATE()");

        builder.HasIndex(e => e.Codigo).IsUnique();
        builder.HasIndex(e => e.CUIT).IsUnique();
    }
}
```

#### Configuración de entidad (ejemplo Comprobante con SEQUENCE)

```csharp
// NewGest.Infrastructure/Data/Configurations/ComprobanteConfiguration.cs
public class ComprobanteConfiguration : IEntityTypeConfiguration<Comprobante>
{
    public void Configure(EntityTypeBuilder<Comprobante> builder)
    {
        builder.ToTable("Comprobantes", schema: "com");

        builder.HasKey(c => c.IdComprobante);
        builder.Property(c => c.IdComprobante).UseIdentityColumn();

        builder.Property(c => c.Tipo).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.Numero).IsRequired();
        builder.Property(c => c.Fecha).IsRequired();
        builder.Property(c => c.TotalNeto).HasColumnType("DECIMAL(18,2)");
        builder.Property(c => c.TotalIva).HasColumnType("DECIMAL(18,2)");
        builder.Property(c => c.Total).HasColumnType("DECIMAL(18,2)");

        // CAE como owned type
        builder.OwnsOne(c => c.Cae, cae =>
        {
            cae.Property(x => x.Codigo).HasColumnName("CaeCodigo").HasMaxLength(14);
            cae.Property(x => x.Vencimiento).HasColumnName("CaeVencimiento");
        });

        builder.HasOne<Cliente>().WithMany().HasForeignKey(c => c.IdCliente).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Empresa>().WithMany().HasForeignKey(c => c.IdEmpresa).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => new { c.IdEmpresa, c.Tipo, c.Numero }).IsUnique();
    }
}
```

#### Schemas SQL + SEQUENCEs en la migración (no en scripts externos)

Las SEQUENCEs para numeración de comprobantes (reemplazo del `IDCTRLTRAN` VFP) se crean dentro de la migración via `migrationBuilder.Sql()`:

```csharp
// NewGest.Infrastructure/Data/Migrations/XXXXXX_InitialCreate.cs
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // 1. Crear schemas (EF Core no lo hace automático)
        migrationBuilder.Sql("CREATE SCHEMA neg");
        migrationBuilder.Sql("CREATE SCHEMA com");
        migrationBuilder.Sql("CREATE SCHEMA cnt");
        migrationBuilder.Sql("CREATE SCHEMA inv");
        migrationBuilder.Sql("CREATE SCHEMA cfg");
        migrationBuilder.Sql("CREATE SCHEMA aud");

        // 2. EF Core genera el CREATE TABLE de cada entidad configurada
        // (el bloque generado automáticamente va aquí)

        // 3. SEQUENCEs para numeración fiscal (reemplaza IDCTRLTRAN de VFP)
        // Los valores de inicio se leen del ETL desde IDCTRLTRAN.DBF antes de ejecutar
        migrationBuilder.Sql("CREATE SEQUENCE com.SeqFCA_000001 START WITH 1 INCREMENT BY 1 NO CYCLE");
        migrationBuilder.Sql("CREATE SEQUENCE com.SeqFCB_000001 START WITH 1 INCREMENT BY 1 NO CYCLE");
        migrationBuilder.Sql("CREATE SEQUENCE com.SeqFCC_000001 START WITH 1 INCREMENT BY 1 NO CYCLE");
        migrationBuilder.Sql("CREATE SEQUENCE com.SeqRecibos_000001 START WITH 1 INCREMENT BY 1 NO CYCLE");
        migrationBuilder.Sql("CREATE SEQUENCE cnt.SeqAsientos_000001 START WITH 1 INCREMENT BY 1 NO CYCLE");

        // 4. Datos semilla obligatorios
        migrationBuilder.InsertData(
            table: "Empresas", schema: "cfg",
            columns: ["Codigo", "RazonSocial", "CUIT", "CondicionIVA", "Activa"],
            values: new object[] { "000001", "EMPRESA PRINCIPAL", "00-00000000-0", 1, true }
        );
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP SEQUENCE IF EXISTS com.SeqFCA_000001");
        // ... resto de rollback
        migrationBuilder.Sql("DROP SCHEMA IF EXISTS aud");
        migrationBuilder.Sql("DROP SCHEMA IF EXISTS cfg");
        // ... etc
    }
}
```

#### Comandos de uso cotidiano

```bash
# Crear nueva migración (desde la raíz del repo)
dotnet ef migrations add <NombreMigracion> \
  --project NewGest.Infrastructure \
  --startup-project NewGest.Api

# Aplicar migraciones pendientes
dotnet ef database update \
  --project NewGest.Infrastructure \
  --startup-project NewGest.Api

# Ver migraciones aplicadas
dotnet ef migrations list \
  --project NewGest.Infrastructure \
  --startup-project NewGest.Api

# Revertir la última migración
dotnet ef database update <MigracionAnterior> \
  --project NewGest.Infrastructure \
  --startup-project NewGest.Api

# Generar script SQL de las migraciones (para auditoría DBA)
dotnet ef migrations script \
  --project NewGest.Infrastructure \
  --startup-project NewGest.Api \
  --output migrations.sql
```

#### Registro en Program.cs

```csharp
// NewGest.Api/Program.cs
builder.Services.AddDbContext<NewgestDbContext>(opts =>
    opts.UseSqlServer(
        builder.Configuration.GetConnectionString("NewgestDb"),
        sql => sql.MigrationsAssembly("NewGest.Infrastructure")
                  .CommandTimeout(120)  // ETL puede tardar en tablas grandes
    )
);

// Aplicar migraciones al arrancar (solo en desarrollo/staging; en prod usar script)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<NewgestDbContext>().Database.Migrate();
}
```

---

### S0-2 — Script ETL: DBF → SQL Server
**Responsable:** fullstack-db  
**Entregable:** `tools/etl/migrate_all.py`  
**Base existente:** `C:\newgest\webapp_stock\` (extender, no reescribir desde cero)

**Tablas a migrar por orden de dependencia:**

```
1. cfg.Empresas          ← Parametr.DBF
2. neg.Zonas             ← ZONACLIE.DBF
3. neg.Unidades          ← UNIDADES.DBF  
4. neg.GruposArticulos   ← ARTGRUPOS.DBF
5. neg.Articulos         ← STOCK.DBF (con campos memo)
6. neg.Clientes          ← CLIENTES.DBF (con campos memo)
7. neg.Personal          ← PERSONAL.DBF
8. inv.Existencias       ← ARTEXIS.DBF
9. inv.CostosHistoricos  ← COSTOART.DBF
10. com.Comprobantes     ← EMICOMPR.DBF ⚠️ tabla más grande
11. com.MovimientosStock ← MOVSTOCK.DBF
12. com.Pagos            ← PAGOS.DBF
13. com.MediosPago       ← MEDIOPAG.DBF
14. com.Imputaciones     ← IMPUTADO.DBF
15. cnt.PlanCuentas      ← PLANCTA.DBF
16. cnt.Asientos         ← ASIENTOS.DBF
17. cnt.LibroIva         ← LIBROIVA.DBF
18. aud.EventLog         ← AUDITORIA.DBF (histórico)
```

**Reglas ETL críticas:**
- Encoding: `cp1252` → UTF-16 para todos los campos de texto
- `SPACE(n)` → `NULL` (campos char "vacíos" en VFP)
- `CTOD('  /  /    ')` → `NULL` (fechas vacías VFP)
- Campos tipo `G` (General/OLE): intentar migrar, loguear si vacíos
- Campos tipo `M` (Memo): usar `.FPT` correspondiente
- Guardar reporte de anomalías en `tools/etl/logs/migration_anomalies.csv`

---

### S0-3 — Validación de integridad referencial
**Responsable:** qa  
**Entregable:** Script `tools/etl/03_validate_integrity.sql` + reporte HTML

```sql
-- Verificar que todos los IdTran de LIBROIVA existen en EMICOMPR
SELECT COUNT(*) AS HuerfanosLibroIva
FROM cnt.LibroIva l
WHERE NOT EXISTS (SELECT 1 FROM com.Comprobantes c WHERE c.IdTran = l.IdTran);

-- Verificar que todos los clientes en comprobantes existen en maestro
SELECT COUNT(*) AS ClientesSinMaestro
FROM com.Comprobantes co
WHERE NOT EXISTS (SELECT 1 FROM neg.Clientes cl WHERE cl.Codigo = co.CodigoCliente);

-- ... (script completo ~30 validaciones)
```

**Criterio de aceptación S0-3:**
- Cero registros huérfanos en relaciones principales
- Si hay huérfanos: documentarlos, no bloquear (son datos sucios del VFP)

---

### S0-4 — Auditoría: entidad + trigger via migración
**Responsable:** fullstack-db  
**Entregable:** `EventLogConfiguration.cs` + migración `AddAuditTriggers`

La tabla `aud.EventLog` se define como entidad C# y se crea via Code First. Los triggers SQL se agregan en la misma migración con `migrationBuilder.Sql()`.

```csharp
// NewGest.Domain/Entities/Audit/EventLog.cs
public class EventLog
{
    public long IdEvent { get; set; }
    public string Tabla { get; set; } = default!;
    public char Operacion { get; set; }    // 'I', 'U', 'D'
    public int IdEmpresa { get; set; }
    public string IdRegistro { get; set; } = default!;
    public string Datos { get; set; } = default!;  // JSON del registro afectado
    public string Usuario { get; set; } = default!;
    public DateTime Fecha { get; set; }
}

// NewGest.Infrastructure/Data/Configurations/EventLogConfiguration.cs
public class EventLogConfiguration : IEntityTypeConfiguration<EventLog>
{
    public void Configure(EntityTypeBuilder<EventLog> builder)
    {
        builder.ToTable("EventLog", schema: "aud");
        builder.HasKey(e => e.IdEvent);
        builder.Property(e => e.IdEvent).UseIdentityColumn();
        builder.Property(e => e.Tabla).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Operacion).IsRequired().HasColumnType("CHAR(1)");
        builder.Property(e => e.IdRegistro).IsRequired().HasMaxLength(50);
        builder.Property(e => e.Datos).IsRequired();
        builder.Property(e => e.Usuario).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Fecha).IsRequired().HasDefaultValueSql("SYSUTCDATETIME()");
    }
}
```

Los triggers SQL van en una migración separada para mantener el historial limpio:

```bash
dotnet ef migrations add AddAuditTriggers \
  --project NewGest.Infrastructure --startup-project NewGest.Api
```

```csharp
// En la migración generada, agregar en Up():
migrationBuilder.Sql("""
    CREATE TRIGGER trg_Comprobantes_Audit
    ON com.Comprobantes AFTER INSERT, UPDATE, DELETE
    AS BEGIN
        SET NOCOUNT ON;
        INSERT INTO aud.EventLog (Tabla, Operacion, IdEmpresa, IdRegistro, Datos, Usuario, Fecha)
        SELECT
            'com.Comprobantes',
            CASE WHEN EXISTS(SELECT 1 FROM inserted) AND EXISTS(SELECT 1 FROM deleted) THEN 'U'
                 WHEN EXISTS(SELECT 1 FROM inserted) THEN 'I' ELSE 'D' END,
            COALESCE(i.IdEmpresa, d.IdEmpresa),
            CAST(COALESCE(i.IdComprobante, d.IdComprobante) AS NVARCHAR(50)),
            (SELECT * FROM inserted FOR JSON PATH),
            SYSTEM_USER,
            SYSUTCDATETIME()
        FROM inserted i FULL OUTER JOIN deleted d ON i.IdComprobante = d.IdComprobante;
    END
    """);

// En Down():
migrationBuilder.Sql("DROP TRIGGER IF EXISTS com.trg_Comprobantes_Audit");
```

---

### S0-5 — Servicio de sincronización VFP ↔ SQL
**Responsable:** fullstack-db  
**Entregable:** `tools/sync/SyncService/` (Windows Service .NET)  
**Propósito:** mientras VFP siga activo, mantener SQL Server actualizado

**Estrategia:**
- VFP escribe DBF → File watcher detecta cambio → INSERT/UPDATE en SQL
- Frecuencia: cada 60 segundos para tablas maestras; tiempo real para comprobantes
- **Importante:** solo para tablas maestras (clientes, artículos). Los comprobantes NO se sincronizan bidireccionalmente hasta el cutover del módulo.
- Window de inconsistencia aceptable: ~60 seg para maestros

---

## Criterios de aceptación del Sprint 0

- [ ] SQL Server corriendo en servidor on-premise, accesible desde la red LAN
- [ ] Todos los DBF de `EM000001` migrados a SQL Server sin errores fatales
- [ ] Reporte de anomalías generado y revisado por el DBA
- [ ] Integridad referencial verificada (script S0-3 ejecutado)
- [ ] Triggers de auditoría activos en schemas `com`, `cnt`, `cfg`
- [ ] ETL es idempotente: puede re-ejecutarse sin duplicar datos (usar MERGE o DELETE+INSERT)
- [ ] Servicio de sincronización corriendo como Windows Service
- [ ] Los archivos `.p12` de certificados AFIP movidos a ubicación segura (fuera de `C:\newgest\certificado\`)
- [ ] Backup automático configurado en SQL Server (full diario + log cada hora)

---

## Riesgos del Sprint 0

| Riesgo | Probabilidad | Acción |
|---|---|---|
| Datos corruptos en DBF | Media | ETL loguea y omite, no falla |
| Campos Memo sin .FPT | Baja | Ignorar silenciosamente, loguear |
| Secuencias desincronizadas con VFP | Media | Sprint 0 solo lee; VFP sigue escribiendo |

---

## Dependencias

- Ninguna (este sprint es el punto de partida)
- **Bloquea:** todos los demás sprints

---

## Notas para el agente fullstack-db

- El ETL existente en `C:\newgest\webapp_stock\` ya tiene parte de la lógica para `STOCK.DBF`. Extenderlo.
- No crear las foreign keys en SQL Server hasta completar el Sprint 0-3 (validación de integridad). Agregarlas después.
- El campo `IDCTRLTRAN` en VFP tiene **1 sola fila** con todos los últimos números. Leerlo antes de crear las SEQUENCES para que arranquen desde el número correcto + 1.

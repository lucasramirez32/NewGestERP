# SPRINT 1–2 — Autenticación, Seguridad y Parámetros
**Semanas:** 5–12 · **Módulos:** M02 (Autenticación) + M20 (Parámetros generales)  
**Fase de coexistencia:** A (VFP sigue productivo)  
**Agente principal:** `fullstack-db` · **Agente QA:** `qa`  
**Riesgo:** Alto — seguridad es la base de todos los módulos posteriores

---

## Objetivo

Implementar el sistema de autenticación en .NET reemplazando el cifrado XOR reversible de VFP por BCrypt + JWT. Migrar la gestión de parámetros del sistema. Al final de este sprint, el equipo puede hacer login en la app .NET.

---

## Contexto VFP (leer antes de codificar)

| Archivo VFP | Función relevante | Problema identificado |
|---|---|---|
| `FUNCION.PRG:267–288` | `FUNCTION Encrypt / Desencry` | XOR reversible — contraseñas recuperables |
| `FUNCION.PRG:735–756` | `PROCEDURE CLAVES` | SEEK en DBF en red — race condition |
| `LOGON.SCX` | Pantalla de login | Variable global `X_Usuario` |
| `ADMIN_USUARIOS.SCX` | ABM de usuarios | Variable global `V_EMPRESA` |
| `MAESTRO_PERMISOS.SCX` | Tabla de permisos por módulo | Acceso directo a `Permisos.DBF` |
| `Parametr.DBF` | Configuración multiempresa | 1 archivo en `C:\newgest\` raíz |

**Acción obligatoria antes de codificar:** las contraseñas VFP **NO pueden migrarse** (el cifrado XOR es reversible pero no es BCrypt). En el primer login, el sistema .NET forzará un reset de contraseña.

---

## Sprint 1 — Autenticación base (Semanas 5–8)

### Tareas S1

#### S1-1 — Entidades de dominio: User + Role
**Entregable:** `NewGest.Domain/Entities/Users/`

```csharp
public class ApplicationUser : IdentityUser<int>
{
    public int IdEmpresa { get; set; }
    public string Nombre { get; set; } = default!;
    public string? Legajo { get; set; }
    public bool DebeResetearPassword { get; set; } = true; // VFP migrants
    public DateTime? UltimoLogin { get; set; }
    public int IntentosFallidos { get; set; }
    public bool Bloqueado { get; set; }
}

public class ApplicationRole : IdentityRole<int>
{
    public int IdEmpresa { get; set; }
}
```

#### S1-2 — Módulo de permisos (reemplaza Permisos.DBF)
**Entregable:** `NewGest.Domain/Entities/Users/Permiso.cs` + `NewGest.Infrastructure/Data/Migrations/`

Los permisos en VFP son por (Usuario, Módulo, Opción). Migrar al sistema de Claims de ASP.NET Core Identity.

```csharp
// Claim personalizado: "newgest:permiso" → "M04:facturar"
public static class NewgestClaims
{
    public const string Permiso = "newgest:permiso";
    public const string Empresa = "newgest:empresa";
    public const string Nombre   = "newgest:nombre";
}

// Policy-based authorization
services.AddAuthorization(opts =>
{
    opts.AddPolicy("PuedeFacturar",
        p => p.RequireClaim(NewgestClaims.Permiso, "M04:facturar"));
    opts.AddPolicy("PuedeAnularComprobante",
        p => p.RequireClaim(NewgestClaims.Permiso, "M04:anular"));
    // ... un policy por permiso granular de VFP
});
```

#### S1-3 — JWT Service
**Entregable:** `NewGest.Infrastructure/Auth/JwtService.cs`

```csharp
public class JwtService : IJwtService
{
    public string GenerarToken(ApplicationUser user, IList<Claim> claims)
    {
        var tokenClaims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(NewgestClaims.Empresa, user.IdEmpresa.ToString()),
            new(NewgestClaims.Nombre, user.Nombre),
        };
        tokenClaims.AddRange(claims);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Secret"]!));
        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: tokenClaims,
            expires: DateTime.UtcNow.AddMinutes(_config.GetValue<int>("Jwt:ExpiryMinutes")),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

#### S1-4 — Endpoint de login (Minimal API)
**Entregable:** `NewGest.Api/Endpoints/AuthEndpoints.cs`

```csharp
app.MapPost("/api/auth/login", async (LoginDto dto, IMediator mediator) =>
{
    var result = await mediator.Send(new LoginCommand(dto.Usuario, dto.Password, dto.Empresa));
    return result.IsSuccess ? Results.Ok(result.Value) : Results.Unauthorized();
});

app.MapPost("/api/auth/refresh", ...);
app.MapPost("/api/auth/cambiar-password", ...); // obligatorio para migrantes VFP
```

#### S1-5 — Migración de usuarios desde DBF
**Entregable:** Script Python `tools/etl/migrate_users.py`

```python
# Lee USUARIOS.DBF (o la tabla equivalente en VFP)
# Crea usuarios en SQL con:
#   - Password temporal aleatoria (no la de VFP)
#   - DebeResetearPassword = True
#   - Permisos mapeados desde Permisos.DBF → Claims
# Envía email con contraseña temporal al usuario (MailKit)
```

---

## Sprint 2 — UI Login + Parámetros (Semanas 9–12)

### Tareas S2

#### S2-1 — Pantalla de login (HTML + Tailwind)
**Entregable:** `NewGest.Web/src/pages/login.html` + `NewGest.Web/src/js/pages/login.js`

UX similar al `LOGON.SCX` de VFP:
- Campo: Empresa (lista desplegable de empresas del usuario — cargada via `GET /api/auth/empresas`)
- Campo: Usuario
- Campo: Contraseña (con toggle mostrar/ocultar)
- Al recibir JWT: guardarlo en `localStorage` como `newgest_token`
- Si `debeResetearPassword = true` en el JWT Claims: redirigir a `cambiar-password.html`

```html
<!-- login.html — estructura mínima con Tailwind -->
<div class="min-h-screen bg-gray-100 flex items-center justify-center">
  <div class="bg-white rounded-2xl shadow-lg p-8 w-full max-w-sm">
    <h1 class="text-2xl font-bold text-gray-800 mb-6">NewGest ERP</h1>
    <form id="form-login" class="space-y-4">
      <select name="empresa" required class="w-full border rounded-lg px-3 py-2 text-sm"></select>
      <input name="usuario" type="text" placeholder="Usuario" required
        class="w-full border rounded-lg px-3 py-2 text-sm" />
      <input name="password" type="password" placeholder="Contraseña" required
        class="w-full border rounded-lg px-3 py-2 text-sm" />
      <button type="submit"
        class="w-full bg-blue-600 hover:bg-blue-700 text-white py-2 rounded-lg font-medium text-sm">
        Ingresar
      </button>
    </form>
  </div>
</div>
```

#### S2-2 — Módulo M20 — Parámetros generales
**Entregable:** `NewGest.Web/Pages/Config/Parametros.razor` + endpoint API

Migrar `Parametr.DBF` al schema `cfg.Parametros`. Este archivo en VFP está en la raíz `C:\newgest\` y es compartido por todas las empresas.

```csharp
public class Parametro
{
    public int IdParametro { get; set; }
    public int? IdEmpresa { get; set; }    // null = global, int = empresa específica
    public string Clave { get; set; } = default!;
    public string Valor { get; set; } = default!;
    public string Descripcion { get; set; } = default!;
}
```

#### S2-3 — ABM de usuarios
**Entregable:** `NewGest.Web/src/pages/admin/usuarios.html` + `js/pages/admin/usuarios.js`

Reemplaza `ADMIN_USUARIOS.SCX`. Funcionalidades:
- Crear/editar/desactivar usuarios
- Asignar permisos por módulo (grilla similar a `MAESTRO_PERMISOS.SCX`)
- Resetear contraseña
- Ver historial de logins (tabla `aud.EventLog`)

---

## Tests — Agente QA

### Unitarios (S1)
```
✓ Login correcto → JWT válido con Claims correctas
✓ Login incorrecto → 401
✓ 5 intentos fallidos → cuenta bloqueada
✓ Usuario de Empresa A no puede acceder a Empresa B
✓ Token expirado → 401
✓ Refresh token renueva correctamente
✓ Cambio de contraseña: la antigua deja de funcionar
```

### Integración (S1-S2)
```
✓ POST /api/auth/login con credenciales válidas → 200 + JWT
✓ GET /api/clientes (M08) sin JWT → 401
✓ GET /api/clientes con JWT sin permiso M08 → 403
✓ Migración de usuarios: todos los usuarios VFP creados con DebeResetearPassword=true
✓ Parametros.DBF migrado completo → verificar count de registros
```

---

## Criterios de aceptación

- [ ] Login exitoso con usuario y contraseña nueva (BCrypt)
- [ ] Login fallido 5 veces bloquea la cuenta (configurable)
- [ ] Permisos por módulo respetados al 100% vs. VFP (comparar Permisos.DBF vs Claims)
- [ ] Sesión expira correctamente por inactividad
- [ ] Auditoría de login/logout registrada en `aud.EventLog`
- [ ] Multi-empresa: usuario de empresa A no ve empresa B
- [ ] Pantalla de cambio de contraseña obligatoria para migrantes VFP
- [ ] `Parametr.DBF` migrado a `cfg.Parametros` sin pérdida
- [ ] JWT Refresh Token funcional

---

## Dependencias

- **Requiere:** Sprint 0 completado (SQL Server + schemas + ETL base)
- **Bloquea:** todos los módulos siguientes (necesitan auth)

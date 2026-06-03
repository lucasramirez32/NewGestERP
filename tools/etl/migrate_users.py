"""
migrate_users.py — Migración de USUARIOS.DBF → cfg schema SQL Server
Sprint 0 / S1-2

Requerimientos:
    pip install dbfread pyodbc bcrypt

Modo de uso:
    python migrate_users.py --dbf-path "C:/VFP/EMPRESA/USUARIOS.DBF"
                            --permisos-dbf "C:/VFP/EMPRESA/PERMISOS.DBF"
                            --connection-string "Server=.;Database=NewGest_Test;..."

Reglas aplicadas (CLAUDE.md):
    - Las contraseñas VFP (XOR reversible) NO se migran.
    - Cada usuario migrado recibe:
        * DebeResetearPassword = True
        * Password temporal aleatoria segura (nunca texto plano en log)
        * IntentosFallidos = 0
        * Bloqueado = False
    - Campos memo: cp1252 → UTF-16 (ya maneja dbfread automáticamente).
    - Fechas vacías VFP → NULL (nunca datetime.min).
"""

import argparse
import secrets
import string
import sys
from datetime import datetime

try:
    import dbfread
    import pyodbc
    import bcrypt
except ImportError as e:
    print(f"ERROR: Falta dependencia — {e}")
    print("Instalar: pip install dbfread pyodbc bcrypt")
    sys.exit(1)


# ─── Configuración de mapeo de módulos VFP → NewGest ─────────────────────────
# Ajustar según PERMISOS.DBF real del cliente
PERMISO_MAP = {
    "FACTU":  "M04",   # Facturación AFIP
    "COBRAN": "M06",   # Cobranzas
    "CLIEN":  "M08",   # Clientes
    "STOCK":  "M09",   # Stock/Inventario
    "ARTICU": "M10",   # Artículos
    "PEDIDO": "M11",   # Pedidos
    "CONTA":  "M12",   # Contabilidad
    "PERSON": "M13",   # Personal
    "REPOR":  "M15",   # Reportes
    "ADMIN":  "ADM",   # Administración
}


def generar_password_temporal(longitud: int = 12) -> str:
    """Genera una contraseña temporal aleatoria segura."""
    alfanumerico = string.ascii_letters + string.digits + "!@#$%"
    return ''.join(secrets.choice(alfanumerico) for _ in range(longitud))


def hashear_password(password: str) -> str:
    """Hashea la contraseña con BCrypt (cost factor 12)."""
    return bcrypt.hashpw(password.encode('utf-8'), bcrypt.gensalt(rounds=12)).decode('utf-8')


def normalize_str(value) -> str | None:
    """Normaliza un string de VFP: strip + None si vacío."""
    if value is None:
        return None
    cleaned = str(value).strip()
    return cleaned if cleaned else None


def normalize_date(value) -> datetime | None:
    """Convierte fecha VFP a datetime, retorna None si vacía o inválida."""
    if value is None:
        return None
    try:
        # dbfread ya convierte a datetime.date; solo filtramos las vacías
        if hasattr(value, 'year') and value.year < 1900:
            return None
        return datetime(value.year, value.month, value.day)
    except (AttributeError, ValueError):
        return None


def leer_permisos_por_usuario(permisos_dbf_path: str) -> dict[str, list[str]]:
    """
    Lee PERMISOS.DBF y retorna un dict: { username → [permiso_newgest, ...] }
    Ajustar según estructura real de la tabla.
    """
    permisos_por_usuario: dict[str, list[str]] = {}

    try:
        table = dbfread.DBF(permisos_dbf_path, encoding='cp1252', ignore_missing_memofile=True)
        for record in table:
            usuario = normalize_str(record.get('USUARIO') or record.get('USER'))
            modulo  = normalize_str(record.get('MODULO') or record.get('MODULE'))
            if not usuario or not modulo:
                continue
            permiso_newgest = PERMISO_MAP.get(modulo.upper())
            if permiso_newgest:
                permisos_por_usuario.setdefault(usuario, []).append(permiso_newgest)
    except FileNotFoundError:
        print(f"  ADVERTENCIA: No se encontró {permisos_dbf_path} — se migrará sin permisos")

    return permisos_por_usuario


def migrar(dbf_path: str, permisos_dbf_path: str, connection_string: str, id_empresa: int,
           dry_run: bool = False) -> None:
    """Ejecuta la migración de usuarios."""

    print(f"[{datetime.now():%H:%M:%S}] Iniciando migración de usuarios")
    print(f"  DBF origen:   {dbf_path}")
    print(f"  IdEmpresa:    {id_empresa}")
    print(f"  Dry run:      {dry_run}")
    print()

    # Cargar permisos
    permisos_map = leer_permisos_por_usuario(permisos_dbf_path) if permisos_dbf_path else {}

    # Conectar a SQL Server
    conn = pyodbc.connect(connection_string)
    cursor = conn.cursor()

    # Leer usuarios de VFP
    table = dbfread.DBF(dbf_path, encoding='cp1252', ignore_missing_memofile=True)

    migrados = 0
    omitidos = 0
    errores = 0

    for record in table:
        try:
            # Campos de USUARIOS.DBF — ajustar según estructura real
            username = normalize_str(record.get('USUARIO') or record.get('USERNAME'))
            nombre   = normalize_str(record.get('NOMBRE') or record.get('NAME'))
            legajo   = normalize_str(record.get('LEGAJO') or record.get('LEGAJ'))

            if not username:
                omitidos += 1
                continue

            # Contraseña temporal (la VFP XOR no se migra — ver CLAUDE.md regla 2)
            password_temp = generar_password_temporal()
            password_hash = hashear_password(password_temp)

            if not dry_run:
                # Insertar en cfg.Users (ASP.NET Identity)
                cursor.execute("""
                    INSERT INTO cfg.Users (
                        UserName, NormalizedUserName, PasswordHash, SecurityStamp,
                        ConcurrencyStamp, Nombre, Legajo, IdEmpresa,
                        DebeResetearPassword, IntentosFallidos, Bloqueado,
                        EmailConfirmed, PhoneNumberConfirmed, TwoFactorEnabled,
                        LockoutEnabled, AccessFailedCount
                    )
                    VALUES (?, UPPER(?), ?, NEWID(), NEWID(), ?, ?, ?,
                            1, 0, 0,
                            0, 0, 0,
                            0, 0)
                """,
                username, username, password_hash,
                nombre or username, legajo, id_empresa)

                user_id = cursor.execute(
                    "SELECT SCOPE_IDENTITY()"
                ).fetchval()

                # Insertar claims de permisos
                for permiso in permisos_map.get(username, []):
                    cursor.execute("""
                        INSERT INTO cfg.UserClaims (UserId, ClaimType, ClaimValue)
                        VALUES (?, 'newgest:permiso', ?)
                    """, user_id, permiso)

            print(f"  [OK] {username} ({nombre}) — permisos: {permisos_map.get(username, [])}")
            migrados += 1

        except Exception as ex:
            print(f"  [ERR] {record}: {ex}")
            errores += 1

    if not dry_run:
        conn.commit()

    conn.close()

    print()
    print(f"[{datetime.now():%H:%M:%S}] Migración finalizada")
    print(f"  Migrados:   {migrados}")
    print(f"  Omitidos:   {omitidos}")
    print(f"  Errores:    {errores}")
    if dry_run:
        print("  (Dry run — no se realizaron cambios en la base de datos)")


# ─── Entry point ─────────────────────────────────────────────────────────────
if __name__ == '__main__':
    parser = argparse.ArgumentParser(description='Migración USUARIOS.DBF → NewGest SQL Server')
    parser.add_argument('--dbf-path', required=True, help='Ruta al archivo USUARIOS.DBF')
    parser.add_argument('--permisos-dbf', default='', help='Ruta al archivo PERMISOS.DBF (opcional)')
    parser.add_argument('--connection-string', required=True, help='Cadena de conexión SQL Server')
    parser.add_argument('--id-empresa', type=int, default=1, help='IdEmpresa para los usuarios migrados')
    parser.add_argument('--dry-run', action='store_true', help='Solo simular, no escribir en BD')

    args = parser.parse_args()
    migrar(
        dbf_path=args.dbf_path,
        permisos_dbf_path=args.permisos_dbf,
        connection_string=args.connection_string,
        id_empresa=args.id_empresa,
        dry_run=args.dry_run,
    )

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NewGest.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate_Auth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "cfg");

            migrationBuilder.CreateTable(
                name: "Empresas",
                schema: "cfg",
                columns: table => new
                {
                    IdEmpresa = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RazonSocial = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Cuit = table.Column<string>(type: "CHAR(13)", nullable: true),
                    Activa = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Empresas", x => x.IdEmpresa);
                });

            migrationBuilder.CreateTable(
                name: "Parametros",
                schema: "cfg",
                columns: table => new
                {
                    IdParametro = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdEmpresa = table.Column<int>(type: "int", nullable: true),
                    Clave = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Valor = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parametros", x => x.IdParametro);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                schema: "cfg",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdEmpresa = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "cfg",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdEmpresa = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Legajo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DebeResetearPassword = table.Column<bool>(type: "bit", nullable: false),
                    UltimoLogin = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IntentosFallidos = table.Column<int>(type: "int", nullable: false),
                    Bloqueado = table.Column<bool>(type: "bit", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RoleClaims",
                schema: "cfg",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleClaims_Roles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "cfg",
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserClaims",
                schema: "cfg",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserClaims_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "cfg",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserLogins",
                schema: "cfg",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_UserLogins_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "cfg",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                schema: "cfg",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "cfg",
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "cfg",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserTokens",
                schema: "cfg",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_UserTokens_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "cfg",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Empresas_Nombre",
                schema: "cfg",
                table: "Empresas",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Parametros_Clave_IdEmpresa",
                schema: "cfg",
                table: "Parametros",
                columns: new[] { "Clave", "IdEmpresa" },
                unique: true,
                filter: "[IdEmpresa] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RoleClaims_RoleId",
                schema: "cfg",
                table: "RoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                schema: "cfg",
                table: "Roles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserClaims_UserId",
                schema: "cfg",
                table: "UserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLogins_UserId",
                schema: "cfg",
                table: "UserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                schema: "cfg",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                schema: "cfg",
                table: "Users",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_Users_IdEmpresa_UserName",
                schema: "cfg",
                table: "Users",
                columns: new[] { "IdEmpresa", "UserName" },
                unique: true,
                filter: "[UserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                schema: "cfg",
                table: "Users",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            // ─── Schemas adicionales (neg, com, cnt, inv, aud) ───────────────
            // Se crean aquí para que estén disponibles en migraciones posteriores
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'neg') EXEC('CREATE SCHEMA neg')");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'com') EXEC('CREATE SCHEMA com')");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'cnt') EXEC('CREATE SCHEMA cnt')");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'inv') EXEC('CREATE SCHEMA inv')");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'aud') EXEC('CREATE SCHEMA aud')");

            // ─── Tabla de auditoría (aud.EventLog) ───────────────────────────
            // Registra todos los INSERT/UPDATE/DELETE en schemas com.*, cnt.*, cfg.*
            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM sys.tables t
                               JOIN sys.schemas s ON t.schema_id = s.schema_id
                               WHERE s.name = 'aud' AND t.name = 'EventLog')
                BEGIN
                    CREATE TABLE aud.EventLog (
                        IdEventLog  BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        Fecha       DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
                        IdEmpresa   INT NULL,
                        IdUsuario   INT NULL,
                        Accion      NVARCHAR(10) NOT NULL,   -- INSERT / UPDATE / DELETE
                        Schema_     NVARCHAR(50) NOT NULL,
                        Tabla       NVARCHAR(100) NOT NULL,
                        ClaveId     NVARCHAR(100) NULL,
                        Datos       NVARCHAR(MAX) NULL       -- JSON del registro afectado
                    );
                    CREATE INDEX IX_EventLog_Fecha ON aud.EventLog (Fecha DESC);
                    CREATE INDEX IX_EventLog_Tabla ON aud.EventLog (Schema_, Tabla, Fecha DESC);
                END
                """);

            // ─── Trigger de auditoría para cfg.Users (logins y cambios) ─────
            migrationBuilder.Sql("""
                IF OBJECT_ID('cfg.trg_Users_Audit', 'TR') IS NOT NULL DROP TRIGGER cfg.trg_Users_Audit;
                EXEC('
                CREATE TRIGGER cfg.trg_Users_Audit
                ON cfg.Users
                AFTER INSERT, UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;
                    DECLARE @accion NVARCHAR(10);
                    IF EXISTS (SELECT 1 FROM inserted) AND EXISTS (SELECT 1 FROM deleted)
                        SET @accion = ''UPDATE'';
                    ELSE IF EXISTS (SELECT 1 FROM inserted)
                        SET @accion = ''INSERT'';
                    ELSE
                        SET @accion = ''DELETE'';

                    INSERT INTO aud.EventLog (IdEmpresa, Accion, Schema_, Tabla, ClaveId)
                    SELECT
                        COALESCE(i.IdEmpresa, d.IdEmpresa),
                        @accion,
                        ''cfg'',
                        ''Users'',
                        CAST(COALESCE(i.Id, d.Id) AS NVARCHAR(100))
                    FROM inserted i
                    FULL OUTER JOIN deleted d ON i.Id = d.Id;
                END');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Empresas",
                schema: "cfg");

            migrationBuilder.DropTable(
                name: "Parametros",
                schema: "cfg");

            migrationBuilder.DropTable(
                name: "RoleClaims",
                schema: "cfg");

            migrationBuilder.DropTable(
                name: "UserClaims",
                schema: "cfg");

            migrationBuilder.DropTable(
                name: "UserLogins",
                schema: "cfg");

            migrationBuilder.DropTable(
                name: "UserRoles",
                schema: "cfg");

            migrationBuilder.DropTable(
                name: "UserTokens",
                schema: "cfg");

            migrationBuilder.DropTable(
                name: "Roles",
                schema: "cfg");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "cfg");

            // Trigger de auditoría
            migrationBuilder.Sql("IF OBJECT_ID('cfg.trg_Users_Audit', 'TR') IS NOT NULL DROP TRIGGER cfg.trg_Users_Audit");

            // Tabla de auditoría
            migrationBuilder.Sql("IF OBJECT_ID('aud.EventLog', 'U') IS NOT NULL DROP TABLE aud.EventLog");
        }
    }
}

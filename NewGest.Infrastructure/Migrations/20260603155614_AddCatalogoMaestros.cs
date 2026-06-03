using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NewGest.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogoMaestros : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "neg");

            migrationBuilder.CreateTable(
                name: "Clientes",
                schema: "neg",
                columns: table => new
                {
                    IdCliente = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdEmpresa = table.Column<int>(type: "int", nullable: false),
                    Codigo = table.Column<string>(type: "CHAR(10)", nullable: false),
                    RazonSocial = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CUIT = table.Column<string>(type: "CHAR(11)", nullable: true),
                    CondicionIva = table.Column<int>(type: "int", nullable: false),
                    Domicilio = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Localidad = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Telefono = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IdZona = table.Column<int>(type: "int", nullable: true),
                    Observaciones = table.Column<string>(type: "NVARCHAR(MAX)", nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clientes", x => x.IdCliente);
                });

            migrationBuilder.CreateTable(
                name: "GruposArticulos",
                schema: "neg",
                columns: table => new
                {
                    IdGrupo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdEmpresa = table.Column<int>(type: "int", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IdGrupoPadre = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GruposArticulos", x => x.IdGrupo);
                    table.ForeignKey(
                        name: "FK_GruposArticulos_GruposArticulos_IdGrupoPadre",
                        column: x => x.IdGrupoPadre,
                        principalSchema: "neg",
                        principalTable: "GruposArticulos",
                        principalColumn: "IdGrupo",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Unidades",
                schema: "neg",
                columns: table => new
                {
                    IdUnidad = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Descripcion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Simbolo = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Unidades", x => x.IdUnidad);
                });

            migrationBuilder.CreateTable(
                name: "Articulos",
                schema: "neg",
                columns: table => new
                {
                    IdArticulo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdEmpresa = table.Column<int>(type: "int", nullable: false),
                    Codigo = table.Column<string>(type: "CHAR(15)", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IdGrupo = table.Column<int>(type: "int", nullable: false),
                    IdUnidad = table.Column<int>(type: "int", nullable: false),
                    PrecioLista = table.Column<decimal>(type: "DECIMAL(18,4)", nullable: false),
                    PrecioCosto = table.Column<decimal>(type: "DECIMAL(18,4)", nullable: false),
                    PorcentajeIva = table.Column<decimal>(type: "DECIMAL(5,2)", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    Observaciones = table.Column<string>(type: "NVARCHAR(MAX)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Articulos", x => x.IdArticulo);
                    table.ForeignKey(
                        name: "FK_Articulos_GruposArticulos_IdGrupo",
                        column: x => x.IdGrupo,
                        principalSchema: "neg",
                        principalTable: "GruposArticulos",
                        principalColumn: "IdGrupo",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Articulos_Unidades_IdUnidad",
                        column: x => x.IdUnidad,
                        principalSchema: "neg",
                        principalTable: "Unidades",
                        principalColumn: "IdUnidad",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Articulos_IdEmpresa_Codigo",
                schema: "neg",
                table: "Articulos",
                columns: new[] { "IdEmpresa", "Codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Articulos_IdGrupo",
                schema: "neg",
                table: "Articulos",
                column: "IdGrupo");

            migrationBuilder.CreateIndex(
                name: "IX_Articulos_IdUnidad",
                schema: "neg",
                table: "Articulos",
                column: "IdUnidad");

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_IdEmpresa_Codigo",
                schema: "neg",
                table: "Clientes",
                columns: new[] { "IdEmpresa", "Codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_IdEmpresa_CUIT",
                schema: "neg",
                table: "Clientes",
                columns: new[] { "IdEmpresa", "CUIT" },
                filter: "[CUIT] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_GruposArticulos_IdEmpresa_Descripcion_IdGrupoPadre",
                schema: "neg",
                table: "GruposArticulos",
                columns: new[] { "IdEmpresa", "Descripcion", "IdGrupoPadre" },
                unique: true,
                filter: "[IdGrupoPadre] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_GruposArticulos_IdGrupoPadre",
                schema: "neg",
                table: "GruposArticulos",
                column: "IdGrupoPadre");

            migrationBuilder.CreateIndex(
                name: "IX_Unidades_Descripcion",
                schema: "neg",
                table: "Unidades",
                column: "Descripcion",
                unique: true);

            // ─── Datos semilla: Unidades de medida base ───────────────────────────────
            migrationBuilder.Sql(@"
SET IDENTITY_INSERT neg.Unidades ON;
INSERT INTO neg.Unidades (IdUnidad, Descripcion, Simbolo) VALUES
  (1, N'Unidad',  N'U'),
  (2, N'Kilogramo', N'Kg'),
  (3, N'Litro',   N'Lt'),
  (4, N'Caja',    N'Cj'),
  (5, N'Par',     N'Par'),
  (6, N'Metro',   N'Mt');
SET IDENTITY_INSERT neg.Unidades OFF;
");

            // ─── Trigger de auditoría: neg.Clientes ───────────────────────────────────
            migrationBuilder.Sql("""
                IF OBJECT_ID('neg.trg_Clientes_Audit', 'TR') IS NOT NULL DROP TRIGGER neg.trg_Clientes_Audit;
                EXEC('
                CREATE TRIGGER neg.trg_Clientes_Audit
                ON neg.Clientes
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
                        ''neg'',
                        ''Clientes'',
                        CAST(COALESCE(i.IdCliente, d.IdCliente) AS NVARCHAR(100))
                    FROM inserted i
                    FULL OUTER JOIN deleted d ON i.IdCliente = d.IdCliente;
                END
                ');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ─── Eliminar trigger y semilla ───────────────────────────────────────────
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS neg.trg_Clientes_Audit;");

            migrationBuilder.DropTable(
                name: "Articulos",
                schema: "neg");

            migrationBuilder.DropTable(
                name: "Clientes",
                schema: "neg");

            migrationBuilder.DropTable(
                name: "GruposArticulos",
                schema: "neg");

            migrationBuilder.DropTable(
                name: "Unidades",
                schema: "neg");
        }
    }
}

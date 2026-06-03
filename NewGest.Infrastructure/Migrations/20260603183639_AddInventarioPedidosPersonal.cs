using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NewGest.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInventarioPedidosPersonal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "inv");

            migrationBuilder.EnsureSchema(
                name: "com");

            migrationBuilder.CreateTable(
                name: "Depositos",
                schema: "inv",
                columns: table => new
                {
                    IdDeposito = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdEmpresa = table.Column<int>(type: "int", nullable: false),
                    Codigo = table.Column<string>(type: "CHAR(10)", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Depositos", x => x.IdDeposito);
                });

            migrationBuilder.CreateTable(
                name: "Empleados",
                schema: "neg",
                columns: table => new
                {
                    IdEmpleado = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdEmpresa = table.Column<int>(type: "int", nullable: false),
                    Legajo = table.Column<string>(type: "CHAR(10)", nullable: false),
                    ApellidoNombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CUIL = table.Column<string>(type: "CHAR(11)", nullable: true),
                    Rol = table.Column<int>(type: "int", nullable: false),
                    EsVendedor = table.Column<bool>(type: "bit", nullable: false),
                    ComisionPorcentaje = table.Column<decimal>(type: "DECIMAL(5,2)", nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Empleados", x => x.IdEmpleado);
                });

            migrationBuilder.CreateTable(
                name: "ExistenciasDeposito",
                schema: "inv",
                columns: table => new
                {
                    IdExistencia = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdEmpresa = table.Column<int>(type: "int", nullable: false),
                    IdArticulo = table.Column<int>(type: "int", nullable: false),
                    IdDeposito = table.Column<int>(type: "int", nullable: false),
                    Cantidad = table.Column<decimal>(type: "DECIMAL(18,4)", nullable: false),
                    CostoPromedio = table.Column<decimal>(type: "DECIMAL(18,4)", nullable: false),
                    StockMinimo = table.Column<decimal>(type: "DECIMAL(18,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExistenciasDeposito", x => x.IdExistencia);
                });

            migrationBuilder.CreateTable(
                name: "MovimientosStock",
                schema: "inv",
                columns: table => new
                {
                    IdMovimiento = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdEmpresa = table.Column<int>(type: "int", nullable: false),
                    IdArticulo = table.Column<int>(type: "int", nullable: false),
                    IdDeposito = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Cantidad = table.Column<decimal>(type: "DECIMAL(18,4)", nullable: false),
                    CostoUnitario = table.Column<decimal>(type: "DECIMAL(18,4)", nullable: false),
                    NumeroSerie = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IdComprobanteOrigen = table.Column<int>(type: "int", nullable: true),
                    FechaMovimiento = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Observaciones = table.Column<string>(type: "NVARCHAR(MAX)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovimientosStock", x => x.IdMovimiento);
                });

            migrationBuilder.CreateTable(
                name: "Mutuales",
                schema: "neg",
                columns: table => new
                {
                    IdMutual = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdEmpresa = table.Column<int>(type: "int", nullable: false),
                    Codigo = table.Column<string>(type: "CHAR(10)", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mutuales", x => x.IdMutual);
                });

            migrationBuilder.CreateTable(
                name: "Pedidos",
                schema: "com",
                columns: table => new
                {
                    IdPedido = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdEmpresa = table.Column<int>(type: "int", nullable: false),
                    IdCliente = table.Column<int>(type: "int", nullable: false),
                    IdVendedor = table.Column<int>(type: "int", nullable: true),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    FechaPedido = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaEntregaEstimada = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Observaciones = table.Column<string>(type: "NVARCHAR(MAX)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pedidos", x => x.IdPedido);
                });

            migrationBuilder.CreateTable(
                name: "Remitos",
                schema: "com",
                columns: table => new
                {
                    IdRemito = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdEmpresa = table.Column<int>(type: "int", nullable: false),
                    IdPedido = table.Column<int>(type: "int", nullable: false),
                    IdCliente = table.Column<int>(type: "int", nullable: false),
                    FechaRemito = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Observaciones = table.Column<string>(type: "NVARCHAR(MAX)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Remitos", x => x.IdRemito);
                });

            migrationBuilder.CreateTable(
                name: "Viajes",
                schema: "neg",
                columns: table => new
                {
                    IdViaje = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdEmpresa = table.Column<int>(type: "int", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    FechaViaje = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Destino = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Observaciones = table.Column<string>(type: "NVARCHAR(MAX)", nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Viajes", x => x.IdViaje);
                });

            migrationBuilder.CreateTable(
                name: "ItemsPedido",
                schema: "com",
                columns: table => new
                {
                    IdItemPedido = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdPedido = table.Column<int>(type: "int", nullable: false),
                    IdArticulo = table.Column<int>(type: "int", nullable: false),
                    CantidadPedida = table.Column<decimal>(type: "DECIMAL(18,4)", nullable: false),
                    CantidadEntregada = table.Column<decimal>(type: "DECIMAL(18,4)", nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "DECIMAL(18,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemsPedido", x => x.IdItemPedido);
                    table.ForeignKey(
                        name: "FK_ItemsPedido_Pedidos_IdPedido",
                        column: x => x.IdPedido,
                        principalSchema: "com",
                        principalTable: "Pedidos",
                        principalColumn: "IdPedido",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItemsRemito",
                schema: "com",
                columns: table => new
                {
                    IdItemRemito = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdRemito = table.Column<int>(type: "int", nullable: false),
                    IdArticulo = table.Column<int>(type: "int", nullable: false),
                    Cantidad = table.Column<decimal>(type: "DECIMAL(18,4)", nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "DECIMAL(18,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemsRemito", x => x.IdItemRemito);
                    table.ForeignKey(
                        name: "FK_ItemsRemito_Remitos_IdRemito",
                        column: x => x.IdRemito,
                        principalSchema: "com",
                        principalTable: "Remitos",
                        principalColumn: "IdRemito",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Depositos_IdEmpresa_Codigo",
                schema: "inv",
                table: "Depositos",
                columns: new[] { "IdEmpresa", "Codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Empleados_IdEmpresa_EsVendedor",
                schema: "neg",
                table: "Empleados",
                columns: new[] { "IdEmpresa", "EsVendedor" });

            migrationBuilder.CreateIndex(
                name: "IX_Empleados_IdEmpresa_Legajo",
                schema: "neg",
                table: "Empleados",
                columns: new[] { "IdEmpresa", "Legajo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExistenciasDeposito_IdArticulo_IdDeposito",
                schema: "inv",
                table: "ExistenciasDeposito",
                columns: new[] { "IdArticulo", "IdDeposito" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExistenciasDeposito_IdEmpresa_IdArticulo",
                schema: "inv",
                table: "ExistenciasDeposito",
                columns: new[] { "IdEmpresa", "IdArticulo" });

            migrationBuilder.CreateIndex(
                name: "IX_ItemsPedido_IdPedido",
                schema: "com",
                table: "ItemsPedido",
                column: "IdPedido");

            migrationBuilder.CreateIndex(
                name: "IX_ItemsRemito_IdRemito",
                schema: "com",
                table: "ItemsRemito",
                column: "IdRemito");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosStock_IdArticulo_FechaMovimiento",
                schema: "inv",
                table: "MovimientosStock",
                columns: new[] { "IdArticulo", "FechaMovimiento" });

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosStock_IdEmpresa_IdDeposito",
                schema: "inv",
                table: "MovimientosStock",
                columns: new[] { "IdEmpresa", "IdDeposito" });

            migrationBuilder.CreateIndex(
                name: "IX_Mutuales_IdEmpresa_Codigo",
                schema: "neg",
                table: "Mutuales",
                columns: new[] { "IdEmpresa", "Codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_IdEmpresa_Estado",
                schema: "com",
                table: "Pedidos",
                columns: new[] { "IdEmpresa", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_IdEmpresa_IdCliente",
                schema: "com",
                table: "Pedidos",
                columns: new[] { "IdEmpresa", "IdCliente" });

            migrationBuilder.CreateIndex(
                name: "IX_Remitos_IdEmpresa_IdPedido",
                schema: "com",
                table: "Remitos",
                columns: new[] { "IdEmpresa", "IdPedido" });

            migrationBuilder.CreateIndex(
                name: "IX_Viajes_IdEmpresa_FechaViaje",
                schema: "neg",
                table: "Viajes",
                columns: new[] { "IdEmpresa", "FechaViaje" });

            // ─── Datos semilla: Depósito Principal ───────────────────────────────────
            migrationBuilder.Sql(@"
SET IDENTITY_INSERT inv.Depositos ON;
INSERT INTO inv.Depositos (IdDeposito, IdEmpresa, Codigo, Descripcion, Activo)
  VALUES (1, 1, 'PPAL      ', N'Depósito Principal', 1);
SET IDENTITY_INSERT inv.Depositos OFF;
");

            // ─── Trigger de auditoría: neg.Empleados ─────────────────────────────────
            migrationBuilder.Sql("""
                IF OBJECT_ID('neg.trg_Empleados_Audit', 'TR') IS NOT NULL DROP TRIGGER neg.trg_Empleados_Audit;
                EXEC('
                CREATE TRIGGER neg.trg_Empleados_Audit
                ON neg.Empleados
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
                        ''Empleados'',
                        CAST(COALESCE(i.IdEmpleado, d.IdEmpleado) AS NVARCHAR(100))
                    FROM inserted i
                    FULL OUTER JOIN deleted d ON i.IdEmpleado = d.IdEmpleado;
                END
                ');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ─── Limpiar trigger y semilla ────────────────────────────────────────────
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS neg.trg_Empleados_Audit;");

            migrationBuilder.DropTable(
                name: "Depositos",
                schema: "inv");

            migrationBuilder.DropTable(
                name: "Empleados",
                schema: "neg");

            migrationBuilder.DropTable(
                name: "ExistenciasDeposito",
                schema: "inv");

            migrationBuilder.DropTable(
                name: "ItemsPedido",
                schema: "com");

            migrationBuilder.DropTable(
                name: "ItemsRemito",
                schema: "com");

            migrationBuilder.DropTable(
                name: "MovimientosStock",
                schema: "inv");

            migrationBuilder.DropTable(
                name: "Mutuales",
                schema: "neg");

            migrationBuilder.DropTable(
                name: "Viajes",
                schema: "neg");

            migrationBuilder.DropTable(
                name: "Pedidos",
                schema: "com");

            migrationBuilder.DropTable(
                name: "Remitos",
                schema: "com");
        }
    }
}

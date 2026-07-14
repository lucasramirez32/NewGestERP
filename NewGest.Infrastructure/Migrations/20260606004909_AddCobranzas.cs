using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NewGest.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCobranzas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "SaldoPendiente",
                schema: "com",
                table: "Comprobantes",
                type: "DECIMAL(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "AlicuotasRetencion",
                schema: "cfg",
                columns: table => new
                {
                    IdAlicuota = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdEmpresa = table.Column<int>(type: "int", nullable: false),
                    TipoRetencion = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Provincia = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    Porcentaje = table.Column<decimal>(type: "DECIMAL(5,2)", nullable: false),
                    VigenciaDesde = table.Column<DateOnly>(type: "date", nullable: false),
                    VigenciaHasta = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlicuotasRetencion", x => x.IdAlicuota);
                });

            migrationBuilder.CreateTable(
                name: "Pagos",
                schema: "com",
                columns: table => new
                {
                    IdPago = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdEmpresa = table.Column<int>(type: "int", nullable: false),
                    IdCliente = table.Column<int>(type: "int", nullable: false),
                    Numero = table.Column<long>(type: "bigint", nullable: false),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    TotalMedios = table.Column<decimal>(type: "DECIMAL(18,2)", nullable: false),
                    TotalImputado = table.Column<decimal>(type: "DECIMAL(18,2)", nullable: false),
                    SaldoAFavor = table.Column<decimal>(type: "DECIMAL(18,2)", nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Anulado = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pagos", x => x.IdPago);
                });

            migrationBuilder.CreateTable(
                name: "Imputaciones",
                schema: "com",
                columns: table => new
                {
                    IdImputacion = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdPago = table.Column<int>(type: "int", nullable: false),
                    IdComprobante = table.Column<int>(type: "int", nullable: false),
                    Monto = table.Column<decimal>(type: "DECIMAL(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Imputaciones", x => x.IdImputacion);
                    table.ForeignKey(
                        name: "FK_Imputaciones_Pagos_IdPago",
                        column: x => x.IdPago,
                        principalSchema: "com",
                        principalTable: "Pagos",
                        principalColumn: "IdPago",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MediosPago",
                schema: "com",
                columns: table => new
                {
                    IdMedioPago = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdPago = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Monto = table.Column<decimal>(type: "DECIMAL(18,2)", nullable: false),
                    BancoEmisor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NumeroCheque = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    FechaVencimientoCheque = table.Column<DateOnly>(type: "date", nullable: true),
                    NumeroTransferencia = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediosPago", x => x.IdMedioPago);
                    table.ForeignKey(
                        name: "FK_MediosPago_Pagos_IdPago",
                        column: x => x.IdPago,
                        principalSchema: "com",
                        principalTable: "Pagos",
                        principalColumn: "IdPago",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Retenciones",
                schema: "com",
                columns: table => new
                {
                    IdRetencion = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdEmpresa = table.Column<int>(type: "int", nullable: false),
                    IdPago = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Provincia = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    Porcentaje = table.Column<decimal>(type: "DECIMAL(5,2)", nullable: false),
                    BaseImponible = table.Column<decimal>(type: "DECIMAL(18,2)", nullable: false),
                    MontoRetenido = table.Column<decimal>(type: "DECIMAL(18,2)", nullable: false),
                    NumeroFormulario = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Retenciones", x => x.IdRetencion);
                    table.ForeignKey(
                        name: "FK_Retenciones_Pagos_IdPago",
                        column: x => x.IdPago,
                        principalSchema: "com",
                        principalTable: "Pagos",
                        principalColumn: "IdPago",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlicuotasRetencion_IdEmpresa_TipoRetencion_Provincia_VigenciaDesde",
                schema: "cfg",
                table: "AlicuotasRetencion",
                columns: new[] { "IdEmpresa", "TipoRetencion", "Provincia", "VigenciaDesde" },
                unique: true,
                filter: "[Provincia] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Imputaciones_IdPago_IdComprobante",
                schema: "com",
                table: "Imputaciones",
                columns: new[] { "IdPago", "IdComprobante" });

            migrationBuilder.CreateIndex(
                name: "IX_MediosPago_IdPago",
                schema: "com",
                table: "MediosPago",
                column: "IdPago");

            migrationBuilder.CreateIndex(
                name: "IX_Pagos_IdEmpresa_IdCliente",
                schema: "com",
                table: "Pagos",
                columns: new[] { "IdEmpresa", "IdCliente" });

            migrationBuilder.CreateIndex(
                name: "IX_Pagos_IdEmpresa_Numero",
                schema: "com",
                table: "Pagos",
                columns: new[] { "IdEmpresa", "Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Retenciones_IdPago",
                schema: "com",
                table: "Retenciones",
                column: "IdPago");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlicuotasRetencion",
                schema: "cfg");

            migrationBuilder.DropTable(
                name: "Imputaciones",
                schema: "com");

            migrationBuilder.DropTable(
                name: "MediosPago",
                schema: "com");

            migrationBuilder.DropTable(
                name: "Retenciones",
                schema: "com");

            migrationBuilder.DropTable(
                name: "Pagos",
                schema: "com");

            migrationBuilder.DropColumn(
                name: "SaldoPendiente",
                schema: "com",
                table: "Comprobantes");
        }
    }
}

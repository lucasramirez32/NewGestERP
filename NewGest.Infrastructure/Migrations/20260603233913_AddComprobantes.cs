using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NewGest.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddComprobantes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Comprobantes",
                schema: "com",
                columns: table => new
                {
                    IdComprobante = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdEmpresa = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    PuntoVenta = table.Column<int>(type: "int", nullable: false),
                    Numero = table.Column<long>(type: "bigint", nullable: false),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    IdCliente = table.Column<int>(type: "int", nullable: false),
                    RazonSocialCliente = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CuitCliente = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: true),
                    CondicionIvaReceptor = table.Column<int>(type: "int", nullable: false),
                    IdPedidoOrigen = table.Column<int>(type: "int", nullable: true),
                    TotalNeto = table.Column<decimal>(type: "DECIMAL(18,2)", nullable: false),
                    TotalIva = table.Column<decimal>(type: "DECIMAL(18,2)", nullable: false),
                    Total = table.Column<decimal>(type: "DECIMAL(18,2)", nullable: false),
                    CaeCodigo = table.Column<string>(type: "nvarchar(14)", maxLength: 14, nullable: true),
                    CaeFechaVencimiento = table.Column<DateOnly>(type: "date", nullable: true),
                    Anulado = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comprobantes", x => x.IdComprobante);
                });

            migrationBuilder.CreateTable(
                name: "ItemsComprobante",
                schema: "com",
                columns: table => new
                {
                    IdItemComprobante = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdComprobante = table.Column<int>(type: "int", nullable: false),
                    IdArticulo = table.Column<int>(type: "int", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Cantidad = table.Column<decimal>(type: "DECIMAL(18,4)", nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "DECIMAL(18,4)", nullable: false),
                    Alicuota = table.Column<int>(type: "int", nullable: false),
                    SubtotalNeto = table.Column<decimal>(type: "DECIMAL(18,2)", nullable: false),
                    Iva = table.Column<decimal>(type: "DECIMAL(18,2)", nullable: false),
                    Subtotal = table.Column<decimal>(type: "DECIMAL(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemsComprobante", x => x.IdItemComprobante);
                    table.ForeignKey(
                        name: "FK_ItemsComprobante_Comprobantes_IdComprobante",
                        column: x => x.IdComprobante,
                        principalSchema: "com",
                        principalTable: "Comprobantes",
                        principalColumn: "IdComprobante",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Comprobantes_IdEmpresa_Fecha",
                schema: "com",
                table: "Comprobantes",
                columns: new[] { "IdEmpresa", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_Comprobantes_IdEmpresa_IdCliente",
                schema: "com",
                table: "Comprobantes",
                columns: new[] { "IdEmpresa", "IdCliente" });

            migrationBuilder.CreateIndex(
                name: "IX_Comprobantes_IdEmpresa_Tipo_PuntoVenta_Numero",
                schema: "com",
                table: "Comprobantes",
                columns: new[] { "IdEmpresa", "Tipo", "PuntoVenta", "Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemsComprobante_IdComprobante",
                schema: "com",
                table: "ItemsComprobante",
                column: "IdComprobante");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItemsComprobante",
                schema: "com");

            migrationBuilder.DropTable(
                name: "Comprobantes",
                schema: "com");
        }
    }
}

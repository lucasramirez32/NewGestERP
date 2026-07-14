using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NewGest.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNumeradoresComprobante : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NumeradoresComprobante",
                schema: "com",
                columns: table => new
                {
                    IdNumerador = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdEmpresa = table.Column<int>(type: "int", nullable: false),
                    PuntoVenta = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    UltimoNumero = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NumeradoresComprobante", x => x.IdNumerador);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NumeradoresComprobante_IdEmpresa_PuntoVenta_Tipo",
                schema: "com",
                table: "NumeradoresComprobante",
                columns: new[] { "IdEmpresa", "PuntoVenta", "Tipo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NumeradoresComprobante",
                schema: "com");
        }
    }
}

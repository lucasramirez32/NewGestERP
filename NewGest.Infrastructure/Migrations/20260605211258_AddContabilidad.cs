using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NewGest.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddContabilidad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "cnt");

            migrationBuilder.CreateTable(
                name: "Asientos",
                schema: "cnt",
                columns: table => new
                {
                    IdAsiento = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdEmpresa = table.Column<int>(type: "int", nullable: false),
                    Numero = table.Column<long>(type: "bigint", nullable: false),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    TipoAsiento = table.Column<int>(type: "int", nullable: false),
                    IdComprobanteOrigen = table.Column<int>(type: "int", nullable: true),
                    Anulado = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Asientos", x => x.IdAsiento);
                });

            migrationBuilder.CreateTable(
                name: "CuentasContables",
                schema: "cnt",
                columns: table => new
                {
                    IdCuenta = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdEmpresa = table.Column<int>(type: "int", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IdCuentaPadre = table.Column<int>(type: "int", nullable: true),
                    Naturaleza = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    ImputaDirectamente = table.Column<bool>(type: "bit", nullable: false),
                    Activa = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CuentasContables", x => x.IdCuenta);
                });

            migrationBuilder.CreateTable(
                name: "PartidasAsiento",
                schema: "cnt",
                columns: table => new
                {
                    IdPartida = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdAsiento = table.Column<int>(type: "int", nullable: false),
                    IdCuenta = table.Column<int>(type: "int", nullable: false),
                    Debe = table.Column<decimal>(type: "DECIMAL(18,2)", nullable: false),
                    Haber = table.Column<decimal>(type: "DECIMAL(18,2)", nullable: false),
                    Concepto = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartidasAsiento", x => x.IdPartida);
                    table.ForeignKey(
                        name: "FK_PartidasAsiento_Asientos_IdAsiento",
                        column: x => x.IdAsiento,
                        principalSchema: "cnt",
                        principalTable: "Asientos",
                        principalColumn: "IdAsiento",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Asientos_IdComprobanteOrigen",
                schema: "cnt",
                table: "Asientos",
                column: "IdComprobanteOrigen");

            migrationBuilder.CreateIndex(
                name: "IX_Asientos_IdEmpresa_Fecha",
                schema: "cnt",
                table: "Asientos",
                columns: new[] { "IdEmpresa", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_Asientos_IdEmpresa_Numero",
                schema: "cnt",
                table: "Asientos",
                columns: new[] { "IdEmpresa", "Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CuentasContables_IdEmpresa_Codigo",
                schema: "cnt",
                table: "CuentasContables",
                columns: new[] { "IdEmpresa", "Codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CuentasContables_IdEmpresa_IdCuentaPadre",
                schema: "cnt",
                table: "CuentasContables",
                columns: new[] { "IdEmpresa", "IdCuentaPadre" });

            migrationBuilder.CreateIndex(
                name: "IX_PartidasAsiento_IdAsiento_IdCuenta",
                schema: "cnt",
                table: "PartidasAsiento",
                columns: new[] { "IdAsiento", "IdCuenta" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CuentasContables",
                schema: "cnt");

            migrationBuilder.DropTable(
                name: "PartidasAsiento",
                schema: "cnt");

            migrationBuilder.DropTable(
                name: "Asientos",
                schema: "cnt");
        }
    }
}

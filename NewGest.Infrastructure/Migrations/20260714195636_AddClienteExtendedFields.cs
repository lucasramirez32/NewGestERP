using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NewGest.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClienteExtendedFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Alergia",
                schema: "neg",
                table: "Clientes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Alergias",
                schema: "neg",
                table: "Clientes",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CodigoPostal",
                schema: "neg",
                table: "Clientes",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Convulsiones",
                schema: "neg",
                table: "Clientes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "Descuento",
                schema: "neg",
                table: "Clientes",
                type: "DECIMAL(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "DiasMora",
                schema: "neg",
                table: "Clientes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "LimiteCredito",
                schema: "neg",
                table: "Clientes",
                type: "DECIMAL(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "MatriculaMedico",
                schema: "neg",
                table: "Clientes",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Medicacion",
                schema: "neg",
                table: "Clientes",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MedicoCabecera",
                schema: "neg",
                table: "Clientes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NombreFantasia",
                schema: "neg",
                table: "Clientes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NroAfiliado",
                schema: "neg",
                table: "Clientes",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ObraSocial",
                schema: "neg",
                table: "Clientes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Patologia",
                schema: "neg",
                table: "Clientes",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Provincia",
                schema: "neg",
                table: "Clientes",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Tratamiento",
                schema: "neg",
                table: "Clientes",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Alergia",
                schema: "neg",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "Alergias",
                schema: "neg",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "CodigoPostal",
                schema: "neg",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "Convulsiones",
                schema: "neg",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "Descuento",
                schema: "neg",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "DiasMora",
                schema: "neg",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "LimiteCredito",
                schema: "neg",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "MatriculaMedico",
                schema: "neg",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "Medicacion",
                schema: "neg",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "MedicoCabecera",
                schema: "neg",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "NombreFantasia",
                schema: "neg",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "NroAfiliado",
                schema: "neg",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "ObraSocial",
                schema: "neg",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "Patologia",
                schema: "neg",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "Provincia",
                schema: "neg",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "Tratamiento",
                schema: "neg",
                table: "Clientes");
        }
    }
}

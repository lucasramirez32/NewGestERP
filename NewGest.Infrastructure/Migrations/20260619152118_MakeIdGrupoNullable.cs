using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NewGest.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeIdGrupoNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "IdGrupo",
                schema: "neg",
                table: "Articulos",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "IdGrupo",
                schema: "neg",
                table: "Articulos",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}

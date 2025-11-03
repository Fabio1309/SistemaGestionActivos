using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaGestionActivos.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanMantenimiento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlanesMantenimiento",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Titulo = table.Column<string>(type: "TEXT", nullable: false),
                    Tarea = table.Column<string>(type: "TEXT", nullable: false),
                    Frecuencia = table.Column<int>(type: "INTEGER", nullable: false),
                    Intervalo = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaProximaEjecucion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CategoriaId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanesMantenimiento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanesMantenimiento_Categorias_CategoriaId",
                        column: x => x.CategoriaId,
                        principalTable: "Categorias",
                        principalColumn: "categ_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlanesMantenimiento_CategoriaId",
                table: "PlanesMantenimiento",
                column: "CategoriaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlanesMantenimiento");
        }
    }
}

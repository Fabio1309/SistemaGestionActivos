using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaGestionActivos.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAsignacionesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Asignacion_Activos_ActivoId",
                table: "Asignacion");

            migrationBuilder.DropForeignKey(
                name: "FK_Asignacion_AspNetUsers_UsuarioId",
                table: "Asignacion");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Asignacion",
                table: "Asignacion");

            migrationBuilder.RenameTable(
                name: "Asignacion",
                newName: "Asignaciones");

            migrationBuilder.RenameIndex(
                name: "IX_Asignacion_UsuarioId",
                table: "Asignaciones",
                newName: "IX_Asignaciones_UsuarioId");

            migrationBuilder.RenameIndex(
                name: "IX_Asignacion_ActivoId",
                table: "Asignaciones",
                newName: "IX_Asignaciones_ActivoId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Asignaciones",
                table: "Asignaciones",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Asignaciones_Activos_ActivoId",
                table: "Asignaciones",
                column: "ActivoId",
                principalTable: "Activos",
                principalColumn: "activo_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Asignaciones_AspNetUsers_UsuarioId",
                table: "Asignaciones",
                column: "UsuarioId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Asignaciones_Activos_ActivoId",
                table: "Asignaciones");

            migrationBuilder.DropForeignKey(
                name: "FK_Asignaciones_AspNetUsers_UsuarioId",
                table: "Asignaciones");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Asignaciones",
                table: "Asignaciones");

            migrationBuilder.RenameTable(
                name: "Asignaciones",
                newName: "Asignacion");

            migrationBuilder.RenameIndex(
                name: "IX_Asignaciones_UsuarioId",
                table: "Asignacion",
                newName: "IX_Asignacion_UsuarioId");

            migrationBuilder.RenameIndex(
                name: "IX_Asignaciones_ActivoId",
                table: "Asignacion",
                newName: "IX_Asignacion_ActivoId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Asignacion",
                table: "Asignacion",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Asignacion_Activos_ActivoId",
                table: "Asignacion",
                column: "ActivoId",
                principalTable: "Activos",
                principalColumn: "activo_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Asignacion_AspNetUsers_UsuarioId",
                table: "Asignacion",
                column: "UsuarioId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}

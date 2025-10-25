using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaGestionActivos.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOrdenDeTrabajoTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Asignacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ActivoId = table.Column<int>(type: "INTEGER", nullable: false),
                    UsuarioId = table.Column<string>(type: "TEXT", nullable: true),
                    FechaAsignacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaDevolucion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EstadoDevolucion = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Asignacion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Asignacion_Activos_ActivoId",
                        column: x => x.ActivoId,
                        principalTable: "Activos",
                        principalColumn: "activo_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Asignacion_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "OrdenesDeTrabajo",
                columns: table => new
                {
                    ot_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    descripcion_problema = table.Column<string>(type: "TEXT", nullable: false),
                    fecha_creacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    estado_ot = table.Column<int>(type: "INTEGER", nullable: false),
                    comentarios = table.Column<string>(type: "TEXT", nullable: true),
                    activo_id = table.Column<int>(type: "INTEGER", nullable: false),
                    usuario_reporta_id = table.Column<string>(type: "TEXT", nullable: false),
                    tecnico_asignado_id = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrdenesDeTrabajo", x => x.ot_id);
                    table.ForeignKey(
                        name: "FK_OrdenesDeTrabajo_Activos_activo_id",
                        column: x => x.activo_id,
                        principalTable: "Activos",
                        principalColumn: "activo_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrdenesDeTrabajo_AspNetUsers_tecnico_asignado_id",
                        column: x => x.tecnico_asignado_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OrdenesDeTrabajo_AspNetUsers_usuario_reporta_id",
                        column: x => x.usuario_reporta_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Asignacion_ActivoId",
                table: "Asignacion",
                column: "ActivoId");

            migrationBuilder.CreateIndex(
                name: "IX_Asignacion_UsuarioId",
                table: "Asignacion",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesDeTrabajo_activo_id",
                table: "OrdenesDeTrabajo",
                column: "activo_id");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesDeTrabajo_tecnico_asignado_id",
                table: "OrdenesDeTrabajo",
                column: "tecnico_asignado_id");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesDeTrabajo_usuario_reporta_id",
                table: "OrdenesDeTrabajo",
                column: "usuario_reporta_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Asignacion");

            migrationBuilder.DropTable(
                name: "OrdenesDeTrabajo");
        }
    }
}

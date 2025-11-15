using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaGestionActivos.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEstadoActivoToStringToEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Se reemplaza el AlterColumn<int> (que estaba mal generado para SQLite)
            // por un SQL directo que PostgreSQL SÍ entiende, usando la pista "USING".
            migrationBuilder.Sql(@"ALTER TABLE ""Activos"" ALTER COLUMN ""estado"" TYPE integer USING (""estado""::integer);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // También corregimos la migración 'Down' para que use SQL de PostgreSQL.
            // Esto convierte la columna de vuelta a 'text' (tipo string de Postgres).
            migrationBuilder.Sql(@"ALTER TABLE ""Activos"" ALTER COLUMN ""estado"" TYPE text;");
        }
    }
}
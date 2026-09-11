using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomCharInfo.server.Migrations
{
    /// <inheritdoc />
    public partial class AddHookOffsetAndSeriesNameUniqueIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Series_SeriesName",
                table: "Series",
                column: "SeriesName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Hooks_Offset",
                table: "Hooks",
                column: "Offset",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Series_SeriesName",
                table: "Series");

            migrationBuilder.DropIndex(
                name: "IX_Hooks_Offset",
                table: "Hooks");
        }
    }
}

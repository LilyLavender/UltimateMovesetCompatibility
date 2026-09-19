using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomCharInfo.server.Migrations
{
    /// <inheritdoc />
    public partial class AddMovesetEditors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MovesetEditors",
                columns: table => new
                {
                    MovesetId = table.Column<int>(type: "integer", nullable: false),
                    ModderId = table.Column<int>(type: "integer", nullable: false),
                    FullAccess = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovesetEditors", x => new { x.MovesetId, x.ModderId });
                    table.ForeignKey(
                        name: "FK_MovesetEditors_Modders_ModderId",
                        column: x => x.ModderId,
                        principalTable: "Modders",
                        principalColumn: "ModderId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MovesetEditors_Movesets_MovesetId",
                        column: x => x.MovesetId,
                        principalTable: "Movesets",
                        principalColumn: "MovesetId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MovesetEditors_ModderId",
                table: "MovesetEditors",
                column: "ModderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MovesetEditors");
        }
    }
}

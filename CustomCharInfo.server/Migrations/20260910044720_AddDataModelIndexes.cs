using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomCharInfo.server.Migrations
{
    /// <inheritdoc />
    public partial class AddDataModelIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CompatibilityReports_MovesetId1",
                table: "CompatibilityReports");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Token",
                table: "RefreshTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompatibilityReports_MovesetId1_MovesetId2_UserId",
                table: "CompatibilityReports",
                columns: new[] { "MovesetId1", "MovesetId2", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActionLogs_ItemId",
                table: "ActionLogs",
                column: "ItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_Token",
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_CompatibilityReports_MovesetId1_MovesetId2_UserId",
                table: "CompatibilityReports");

            migrationBuilder.DropIndex(
                name: "IX_ActionLogs_ItemId",
                table: "ActionLogs");

            migrationBuilder.CreateIndex(
                name: "IX_CompatibilityReports_MovesetId1",
                table: "CompatibilityReports",
                column: "MovesetId1");
        }
    }
}

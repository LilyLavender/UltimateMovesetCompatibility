using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CustomCharInfo.server.Migrations
{
    /// <inheritdoc />
    public partial class AddContributionRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ShowOnCard",
                table: "MovesetModders",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "ContributionRoles",
                columns: table => new
                {
                    ContributionRoleId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContributionRoles", x => x.ContributionRoleId);
                });

            migrationBuilder.CreateTable(
                name: "MovesetModderRoles",
                columns: table => new
                {
                    MovesetId = table.Column<int>(type: "integer", nullable: false),
                    ModderId = table.Column<int>(type: "integer", nullable: false),
                    ContributionRoleId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovesetModderRoles", x => new { x.MovesetId, x.ModderId, x.ContributionRoleId });
                    table.ForeignKey(
                        name: "FK_MovesetModderRoles_ContributionRoles_ContributionRoleId",
                        column: x => x.ContributionRoleId,
                        principalTable: "ContributionRoles",
                        principalColumn: "ContributionRoleId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MovesetModderRoles_MovesetModders_MovesetId_ModderId",
                        columns: x => new { x.MovesetId, x.ModderId },
                        principalTable: "MovesetModders",
                        principalColumns: new[] { "MovesetId", "ModderId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "ContributionRoles",
                columns: new[] { "ContributionRoleId", "Name" },
                values: new object[,]
                {
                    { 1, "Coding" },
                    { 2, "Animation" },
                    { 3, "Modelling" },
                    { 4, "Rendering" },
                    { 5, "Sounds" },
                    { 6, "Effects" },
                    { 7, "Concept/Design" },
                    { 8, "Other" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_MovesetModderRoles_ContributionRoleId",
                table: "MovesetModderRoles",
                column: "ContributionRoleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MovesetModderRoles");

            migrationBuilder.DropTable(
                name: "ContributionRoles");

            migrationBuilder.DropColumn(
                name: "ShowOnCard",
                table: "MovesetModders");
        }
    }
}

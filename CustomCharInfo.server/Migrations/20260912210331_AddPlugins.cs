using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CustomCharInfo.server.Migrations
{
    /// <inheritdoc />
    public partial class AddPlugins : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Plugins",
                columns: table => new
                {
                    PluginId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    DefaultLearnMoreUrl = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    MovesetId = table.Column<int>(type: "integer", nullable: true),
                    DependencyId = table.Column<int>(type: "integer", nullable: true),
                    OwnerModderId = table.Column<int>(type: "integer", nullable: false),
                    CurrentDeterminationMethod = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plugins", x => x.PluginId);
                    table.CheckConstraint("CK_Plugin_SingleAttachment", "(CASE WHEN \"MovesetId\" IS NOT NULL THEN 1 ELSE 0 END) + (CASE WHEN \"DependencyId\" IS NOT NULL THEN 1 ELSE 0 END) <= 1");
                    table.ForeignKey(
                        name: "FK_Plugins_Dependencies_DependencyId",
                        column: x => x.DependencyId,
                        principalTable: "Dependencies",
                        principalColumn: "DependencyId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Plugins_Modders_OwnerModderId",
                        column: x => x.OwnerModderId,
                        principalTable: "Modders",
                        principalColumn: "ModderId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Plugins_Movesets_MovesetId",
                        column: x => x.MovesetId,
                        principalTable: "Movesets",
                        principalColumn: "MovesetId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PluginVersions",
                columns: table => new
                {
                    PluginVersionId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PluginId = table.Column<int>(type: "integer", nullable: false),
                    VersionLabel = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ReleaseDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    LearnMoreUrl = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    IsCurrent = table.Column<bool>(type: "boolean", nullable: false),
                    SubmittedByUserId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PluginVersions", x => x.PluginVersionId);
                    table.ForeignKey(
                        name: "FK_PluginVersions_ApplicationUser_SubmittedByUserId",
                        column: x => x.SubmittedByUserId,
                        principalTable: "ApplicationUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PluginVersions_Plugins_PluginId",
                        column: x => x.PluginId,
                        principalTable: "Plugins",
                        principalColumn: "PluginId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "ItemTypes",
                columns: new[] { "ItemTypeId", "ItemTypeName" },
                values: new object[] { 5, "Plugin" });

            migrationBuilder.CreateIndex(
                name: "IX_Plugins_DependencyId",
                table: "Plugins",
                column: "DependencyId");

            migrationBuilder.CreateIndex(
                name: "IX_Plugins_MovesetId",
                table: "Plugins",
                column: "MovesetId");

            migrationBuilder.CreateIndex(
                name: "IX_Plugins_OwnerModderId",
                table: "Plugins",
                column: "OwnerModderId");

            migrationBuilder.CreateIndex(
                name: "IX_PluginVersions_Hash",
                table: "PluginVersions",
                column: "Hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PluginVersions_PluginId",
                table: "PluginVersions",
                column: "PluginId");

            migrationBuilder.CreateIndex(
                name: "IX_PluginVersions_SubmittedByUserId",
                table: "PluginVersions",
                column: "SubmittedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PluginVersions");

            migrationBuilder.DropTable(
                name: "Plugins");

            migrationBuilder.DeleteData(
                table: "ItemTypes",
                keyColumn: "ItemTypeId",
                keyValue: 5);
        }
    }
}

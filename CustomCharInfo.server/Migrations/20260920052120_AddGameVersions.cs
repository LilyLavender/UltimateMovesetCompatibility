using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CustomCharInfo.server.Migrations
{
    /// <inheritdoc />
    public partial class AddGameVersions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Hooks_Offset",
                table: "Hooks");

            migrationBuilder.CreateTable(
                name: "GameVersions",
                columns: table => new
                {
                    GameVersionId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameVersions", x => x.GameVersionId);
                    table.ForeignKey(
                        name: "FK_GameVersions_ApplicationUser_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "ApplicationUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "OffsetState",
                columns: table => new
                {
                    OffsetStateId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OffsetState", x => x.OffsetStateId);
                });

            migrationBuilder.CreateTable(
                name: "GameVersionShifts",
                columns: table => new
                {
                    GameVersionShiftId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FromGameVersionId = table.Column<int>(type: "integer", nullable: false),
                    ToGameVersionId = table.Column<int>(type: "integer", nullable: false),
                    RangeStart = table.Column<long>(type: "bigint", nullable: false),
                    RangeEnd = table.Column<long>(type: "bigint", nullable: false),
                    Delta = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameVersionShifts", x => x.GameVersionShiftId);
                    table.ForeignKey(
                        name: "FK_GameVersionShifts_GameVersions_FromGameVersionId",
                        column: x => x.FromGameVersionId,
                        principalTable: "GameVersions",
                        principalColumn: "GameVersionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GameVersionShifts_GameVersions_ToGameVersionId",
                        column: x => x.ToGameVersionId,
                        principalTable: "GameVersions",
                        principalColumn: "GameVersionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HookOffsets",
                columns: table => new
                {
                    HookId = table.Column<int>(type: "integer", nullable: false),
                    GameVersionId = table.Column<int>(type: "integer", nullable: false),
                    Offset = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    OffsetStateId = table.Column<int>(type: "integer", nullable: false),
                    SetByUserId = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HookOffsets", x => new { x.HookId, x.GameVersionId });
                    table.ForeignKey(
                        name: "FK_HookOffsets_ApplicationUser_SetByUserId",
                        column: x => x.SetByUserId,
                        principalTable: "ApplicationUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_HookOffsets_GameVersions_GameVersionId",
                        column: x => x.GameVersionId,
                        principalTable: "GameVersions",
                        principalColumn: "GameVersionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HookOffsets_Hooks_HookId",
                        column: x => x.HookId,
                        principalTable: "Hooks",
                        principalColumn: "HookId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HookOffsets_OffsetState_OffsetStateId",
                        column: x => x.OffsetStateId,
                        principalTable: "OffsetState",
                        principalColumn: "OffsetStateId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "OffsetState",
                columns: new[] { "OffsetStateId", "Name" },
                values: new object[,]
                {
                    { 1, "Confirmed" },
                    { 2, "Generated" },
                    { 3, "Carried Forward" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_GameVersions_CreatedByUserId",
                table: "GameVersions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GameVersions_Name",
                table: "GameVersions",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GameVersions_SortOrder",
                table: "GameVersions",
                column: "SortOrder",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GameVersionShifts_FromGameVersionId_ToGameVersionId_RangeSt~",
                table: "GameVersionShifts",
                columns: new[] { "FromGameVersionId", "ToGameVersionId", "RangeStart" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GameVersionShifts_ToGameVersionId",
                table: "GameVersionShifts",
                column: "ToGameVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_HookOffsets_GameVersionId_Offset",
                table: "HookOffsets",
                columns: new[] { "GameVersionId", "Offset" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HookOffsets_OffsetStateId",
                table: "HookOffsets",
                column: "OffsetStateId");

            migrationBuilder.CreateIndex(
                name: "IX_HookOffsets_SetByUserId",
                table: "HookOffsets",
                column: "SetByUserId");

            // Every existing hook was recorded against 13.0.4, which the site moved to on 2025-06-24.
            // Normalize the stored spelling first (no 0x, uppercase, no leading zeros) so the per-version unique index holds.
            migrationBuilder.Sql(
                "INSERT INTO \"GameVersions\" (\"Name\", \"SortOrder\", \"CreatedByUserId\", \"CreatedAt\") VALUES ('13.0.4', 1, NULL, '2025-06-24 00:00:00+00');");

            migrationBuilder.Sql(
                "UPDATE \"Hooks\" SET \"Offset\" = COALESCE(NULLIF(LTRIM(UPPER(REGEXP_REPLACE(\"Offset\", '^0[xX]', '')), '0'), ''), '0');");

            migrationBuilder.Sql(
                "INSERT INTO \"HookOffsets\" (\"HookId\", \"GameVersionId\", \"Offset\", \"OffsetStateId\", \"SetByUserId\", \"UpdatedAt\") " +
                "SELECT h.\"HookId\", gv.\"GameVersionId\", h.\"Offset\", 1, NULL, '2025-06-24 00:00:00+00' " +
                "FROM \"Hooks\" h CROSS JOIN \"GameVersions\" gv WHERE gv.\"Name\" = '13.0.4';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GameVersionShifts");

            migrationBuilder.DropTable(
                name: "HookOffsets");

            migrationBuilder.DropTable(
                name: "GameVersions");

            migrationBuilder.DropTable(
                name: "OffsetState");

            migrationBuilder.CreateIndex(
                name: "IX_Hooks_Offset",
                table: "Hooks",
                column: "Offset",
                unique: true);
        }
    }
}

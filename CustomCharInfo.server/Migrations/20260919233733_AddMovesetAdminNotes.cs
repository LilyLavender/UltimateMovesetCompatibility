using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomCharInfo.server.Migrations
{
    /// <inheritdoc />
    public partial class AddMovesetAdminNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MovesetAdminNotes",
                columns: table => new
                {
                    MovesetId = table.Column<int>(type: "integer", nullable: false),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    UpdatedByUserId = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovesetAdminNotes", x => x.MovesetId);
                    table.ForeignKey(
                        name: "FK_MovesetAdminNotes_ApplicationUser_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "ApplicationUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MovesetAdminNotes_Movesets_MovesetId",
                        column: x => x.MovesetId,
                        principalTable: "Movesets",
                        principalColumn: "MovesetId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MovesetAdminNotes_UpdatedByUserId",
                table: "MovesetAdminNotes",
                column: "UpdatedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MovesetAdminNotes");
        }
    }
}

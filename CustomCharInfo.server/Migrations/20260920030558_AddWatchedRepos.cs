using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CustomCharInfo.server.Migrations
{
    /// <inheritdoc />
    public partial class AddWatchedRepos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WatchedRepos",
                columns: table => new
                {
                    WatchedRepoId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Owner = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Repo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AddedByUserId = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WatchedRepos", x => x.WatchedRepoId);
                    table.ForeignKey(
                        name: "FK_WatchedRepos_ApplicationUser_AddedByUserId",
                        column: x => x.AddedByUserId,
                        principalTable: "ApplicationUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WatchedRepos_AddedByUserId",
                table: "WatchedRepos",
                column: "AddedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WatchedRepos_Owner_Repo",
                table: "WatchedRepos",
                columns: new[] { "Owner", "Repo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WatchedRepos");
        }
    }
}

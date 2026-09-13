using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CustomCharInfo.server.Migrations
{
    /// <inheritdoc />
    public partial class AddUnknownPluginHashes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UnknownPluginHashes",
                columns: table => new
                {
                    UnknownPluginHashId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CheckCount = table.Column<int>(type: "integer", nullable: false),
                    FirstCheckedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastCheckedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnknownPluginHashes", x => x.UnknownPluginHashId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UnknownPluginHashes_Hash",
                table: "UnknownPluginHashes",
                column: "Hash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UnknownPluginHashes");
        }
    }
}

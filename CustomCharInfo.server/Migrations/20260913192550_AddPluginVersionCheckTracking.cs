using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomCharInfo.server.Migrations
{
    /// <inheritdoc />
    public partial class AddPluginVersionCheckTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CheckCount",
                table: "PluginVersions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastCheckedAt",
                table: "PluginVersions",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CheckCount",
                table: "PluginVersions");

            migrationBuilder.DropColumn(
                name: "LastCheckedAt",
                table: "PluginVersions");
        }
    }
}

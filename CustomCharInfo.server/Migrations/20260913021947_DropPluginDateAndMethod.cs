using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomCharInfo.server.Migrations
{
    /// <inheritdoc />
    public partial class DropPluginDateAndMethod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReleaseDate",
                table: "PluginVersions");

            migrationBuilder.DropColumn(
                name: "CurrentDeterminationMethod",
                table: "Plugins");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "ReleaseDate",
                table: "PluginVersions",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrentDeterminationMethod",
                table: "Plugins",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}

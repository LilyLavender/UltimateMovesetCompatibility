using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomCharInfo.server.Migrations
{
    /// <inheritdoc />
    public partial class AddPluginVersionFirstCheckedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FirstCheckedAt",
                table: "PluginVersions",
                type: "timestamp with time zone",
                nullable: true);

            // One-time backfill. Hashes that were looked up before their version was registered
            // still had an UnknownPluginHashes row; fold its history into the version and drop it.
            // Postgres folds unquoted identifiers to lowercase, so every name is quoted.
            migrationBuilder.Sql("""
                UPDATE "PluginVersions" v
                SET "CheckCount" = v."CheckCount" + u."CheckCount",
                    "FirstCheckedAt" = u."FirstCheckedAt",
                    "LastCheckedAt" = GREATEST(COALESCE(v."LastCheckedAt", u."LastCheckedAt"), u."LastCheckedAt")
                FROM "UnknownPluginHashes" u
                WHERE u."Hash" = v."Hash";

                DELETE FROM "UnknownPluginHashes" u
                USING "PluginVersions" v
                WHERE v."Hash" = u."Hash";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstCheckedAt",
                table: "PluginVersions");
        }
    }
}

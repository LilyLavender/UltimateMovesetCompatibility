using CustomCharInfo.server.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomCharInfo.server.Migrations
{
    // Written by hand: a data-only step with no model change, so it has no Designer file.
    // Databases that ran AddGameVersions before the 13.0.4 date was added to it get the date here.
    // The site moved to 13.0.4 on 2025-06-24; only the rows the migration seeded (no SetByUserId) are touched.
    [DbContext(typeof(AppDbContext))]
    [Migration("20260920120000_SetGameVersion1304Date")]
    public partial class SetGameVersion1304Date : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE \"GameVersions\" SET \"CreatedAt\" = '2025-06-24 00:00:00+00' WHERE \"Name\" = '13.0.4';");

            migrationBuilder.Sql(
                "UPDATE \"HookOffsets\" SET \"UpdatedAt\" = '2025-06-24 00:00:00+00' " +
                "WHERE \"SetByUserId\" IS NULL AND \"GameVersionId\" IN (SELECT \"GameVersionId\" FROM \"GameVersions\" WHERE \"Name\" = '13.0.4');");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The dates this replaced were the moment AddGameVersions ran and are not worth restoring.
        }
    }
}

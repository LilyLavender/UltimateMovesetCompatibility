using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomCharInfo.server.Migrations
{
    /// <inheritdoc />
    public partial class ReleaseDateToDateOnly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ReleaseDate is a calendar day (a mod's release date), not an instant, but was
            // stored as timestamptz. A client-side bug (v-date-picker -> Date -> toISOString())
            // shifted the date back by one day for every user east of UTC before this fix.
            // The default EF-generated cast (bare AlterColumn) truncates through the session's
            // timezone GUC (UTC on Render/Neon) and would silently keep that shift baked in.
            //
            // Correction rule: a row's UTC time-of-day is only ever non-midnight when it came
            // from an east-of-UTC user's local midnight (see docs/planning/2026-09-10_release-
            // date-timezone-fix.md for the derivation) - and in that case the intended date is
            // always exactly one day after the stored UTC date, regardless of the specific
            // offset or DST. Rows already at exact UTC midnight are left untouched.
            migrationBuilder.Sql(@"
                ALTER TABLE ""Movesets""
                ALTER COLUMN ""ReleaseDate"" TYPE date
                USING (
                    CASE
                        WHEN ""ReleaseDate"" IS NULL THEN NULL
                        WHEN (""ReleaseDate"" AT TIME ZONE 'UTC') = date_trunc('day', ""ReleaseDate"" AT TIME ZONE 'UTC')
                            THEN (""ReleaseDate"" AT TIME ZONE 'UTC')::date
                        ELSE ((""ReleaseDate"" AT TIME ZONE 'UTC')::date + 1)
                    END
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Best-effort revert only: the original time-of-day (and therefore which rows were
            // ever shifted) is not recoverable once collapsed to a plain date. This restores
            // each date as UTC midnight, which is not guaranteed to match the pre-Up value.
            migrationBuilder.Sql(@"
                ALTER TABLE ""Movesets""
                ALTER COLUMN ""ReleaseDate"" TYPE timestamp with time zone
                USING (""ReleaseDate""::timestamp AT TIME ZONE 'UTC');
            ");
        }
    }
}

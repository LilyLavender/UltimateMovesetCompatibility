using CustomCharInfo.server.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomCharInfo.server.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261014120000_AddWatchedRepoLastCheck")]
    public partial class AddWatchedRepoLastCheck : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastCheckedAt",
                table: "WatchedRepos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastReleaseAt",
                table: "WatchedRepos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastReleaseTag",
                table: "WatchedRepos",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastReleaseUrl",
                table: "WatchedRepos",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "LastCheckedAt", table: "WatchedRepos");
            migrationBuilder.DropColumn(name: "LastReleaseAt", table: "WatchedRepos");
            migrationBuilder.DropColumn(name: "LastReleaseTag", table: "WatchedRepos");
            migrationBuilder.DropColumn(name: "LastReleaseUrl", table: "WatchedRepos");
        }
    }
}

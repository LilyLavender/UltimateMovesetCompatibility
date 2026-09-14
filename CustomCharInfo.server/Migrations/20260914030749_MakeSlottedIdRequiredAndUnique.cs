using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomCharInfo.server.Migrations
{
    /// <inheritdoc />
    public partial class MakeSlottedIdRequiredAndUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "SlottedId",
                table: "Movesets",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32,
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Movesets_SlottedId_NoDigits",
                table: "Movesets",
                sql: "\"SlottedId\" !~ '[0-9]'");

            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX \"IX_Movesets_SlottedId_Lower\" ON \"Movesets\" (LOWER(\"SlottedId\"));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX \"IX_Movesets_SlottedId_Lower\";");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Movesets_SlottedId_NoDigits",
                table: "Movesets");

            migrationBuilder.AlterColumn<string>(
                name: "SlottedId",
                table: "Movesets",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32);
        }
    }
}

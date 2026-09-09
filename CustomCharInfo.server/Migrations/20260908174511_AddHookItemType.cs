using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomCharInfo.server.Migrations
{
    /// <inheritdoc />
    public partial class AddHookItemType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "ItemTypes",
                columns: new[] { "ItemTypeId", "ItemTypeName" },
                values: new object[] { 4, "Hook" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ItemTypes",
                keyColumn: "ItemTypeId",
                keyValue: 4);
        }
    }
}

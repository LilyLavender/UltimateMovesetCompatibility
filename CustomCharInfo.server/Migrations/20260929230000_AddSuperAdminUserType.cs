using CustomCharInfo.server.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomCharInfo.server.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260929230000_AddSuperAdminUserType")]
    public partial class AddSuperAdminUserType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "INSERT INTO \"UserType\" (\"UserTypeId\", \"UserTypeName\") VALUES (4, 'SuperAdmin');");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM \"UserType\" WHERE \"UserTypeId\" = 4;");
        }
    }
}

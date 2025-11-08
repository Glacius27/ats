using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ats_authorization_service.Migrations
{
    /// <inheritdoc />
    public partial class SeedRoles_Reapply : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { Guid.Parse("11111111-1111-1111-1111-111111111111"), "Admin" },
                    { Guid.Parse("22222222-2222-2222-2222-222222222222"), "Recruiter" },
                    { Guid.Parse("33333333-3333-3333-3333-333333333333"), "HR" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData("Roles", "Id", Guid.Parse("11111111-1111-1111-1111-111111111111"));
            migrationBuilder.DeleteData("Roles", "Id", Guid.Parse("22222222-2222-2222-2222-222222222222"));
            migrationBuilder.DeleteData("Roles", "Id", Guid.Parse("33333333-3333-3333-3333-333333333333"));
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ats.Migrations
{
    /// <inheritdoc />
    public partial class AddCandidatePatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Candidates",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Candidates");
        }
    }
}

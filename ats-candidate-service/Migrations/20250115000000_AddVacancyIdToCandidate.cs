using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CandidateService.Migrations
{
    /// <inheritdoc />
    public partial class AddVacancyIdToCandidate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VacancyId",
                table: "Candidates",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VacancyId",
                table: "Candidates");
        }
    }
}

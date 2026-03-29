using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DuskOfCoding.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPreferredLanguageToSubmission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PreferredLanguage",
                table: "Submissions",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreferredLanguage",
                table: "Submissions");
        }
    }
}

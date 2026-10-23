using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolNewspaperBlazorApp.Migrations
{
    /// <inheritdoc />
    public partial class AddArticleDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Details",
                table: "Articles",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Details",
                table: "Articles");
        }
    }
}

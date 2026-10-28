using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolNewspaperBlazorApp.Migrations
{
    /// <inheritdoc />
    public partial class ArticleImageFocus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ImageFocusX",
                table: "Articles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ImageFocusY",
                table: "Articles",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageFocusX",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "ImageFocusY",
                table: "Articles");
        }
    }
}

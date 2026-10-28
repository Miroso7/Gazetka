using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolNewspaperBlazorApp.Migrations
{
    /// <inheritdoc />
    public partial class ArticleDraftViewsOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDraft",
                table: "Articles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ViewCount",
                table: "Articles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "ArticleImages",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDraft",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "ViewCount",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "ArticleImages");
        }
    }
}

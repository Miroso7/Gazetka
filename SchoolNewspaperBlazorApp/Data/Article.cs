using System.ComponentModel.DataAnnotations;
using SchoolNewspaperBlazorApp.Data.Enums;

namespace SchoolNewspaperBlazorApp.Data
{
    public class Article
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        public required string Text { get; set; }
        public required string Author { get; set; }
        public DateTime PublishDate { get; set; }
        public ArticleType Type { get; set; } = ArticleType.News;
        // pola zależne od typu artykułu (JSON, patrz ArticleFields)
        public string? Details { get; set; }
        public int FileId { get; set; }
        public List<ArticleImage> Images { get; set; } = new();

    }


}

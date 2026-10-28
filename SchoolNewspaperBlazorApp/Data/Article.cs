using System.ComponentModel.DataAnnotations;
using SchoolNewspaperBlazorApp.Data.Enums;

namespace SchoolNewspaperBlazorApp.Data
{
    public class Article : ISoftDelete
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        public required string Text { get; set; }
        public required string Author { get; set; }
        // „Opublikuj od”: artykuł z datą w przyszłości jest zaplanowany i widzi go tylko redakcja
        public DateTime PublishDate { get; set; }
        // „Wyświetlaj do”: po tej dacie artykuł znika z listy i wyszukiwarki, ale pod bezpośrednim linkiem nadal się otwiera (null = bez końca)
        public DateTime? ExpiresAt { get; set; }
        // przypięty artykuł jest zawsze na górze listy
        public bool IsPinned { get; set; }
        public ArticleType Type { get; set; } = ArticleType.News;
        // pola zależne od typu artykułu (JSON, patrz ArticleFields)
        public string? Details { get; set; }
        public int FileId { get; set; }
        public List<ArticleImage> Images { get; set; } = new();

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

    }


}

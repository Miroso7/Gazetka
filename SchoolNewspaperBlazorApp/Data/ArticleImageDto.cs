using SchoolNewspaperBlazorApp.Data.Enums;

namespace SchoolNewspaperBlazorApp.Data
{
    // Zdjęcie z galerii razem z nazwą pliku (do wyświetlenia na stronie i w formularzu).
    public class ArticleImageDto
    {
        public int Id { get; set; }
        public int FileId { get; set; }
        public string FileName { get; set; } = "";
        public InlinePlacement Placement { get; set; }
        public int InlineAfter { get; set; }
        public string Url => $"/Images/{Uri.EscapeDataString(FileName)}";
    }
}

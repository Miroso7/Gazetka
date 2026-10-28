using SchoolNewspaperBlazorApp.Data.Enums;

namespace SchoolNewspaperBlazorApp.Data
{
    // Dodatkowe zdjęcie z galerii artykułu (główne zdjęcie to nadal Article.FileId).
    public class ArticleImage : ISoftDelete
    {
        public int Id { get; set; }
        public int ArticleId { get; set; }
        public int FileId { get; set; }
        // gdzie zdjęcie pokazuje się w artykule (domyślnie tylko w galerii pod tekstem)
        public InlinePlacement Placement { get; set; }
        // zdjęcie w treści pokazuje się po tylu akapitach (0 = przed pierwszym)
        public int InlineAfter { get; set; }
        // kolejność w galerii (rosnąco, przy równych wartościach decyduje Id)
        public int SortOrder { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}

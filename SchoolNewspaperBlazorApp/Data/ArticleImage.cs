namespace SchoolNewspaperBlazorApp.Data
{
    // Dodatkowe zdjęcie z galerii artykułu (główne zdjęcie to nadal Article.FileId).
    public class ArticleImage : ISoftDelete
    {
        public int Id { get; set; }
        public int ArticleId { get; set; }
        public int FileId { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}

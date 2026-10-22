namespace SchoolNewspaperBlazorApp.Data
{
    // Dodatkowe zdjęcie z galerii artykułu (główne zdjęcie to nadal Article.FileId).
    public class ArticleImage
    {
        public int Id { get; set; }
        public int ArticleId { get; set; }
        public int FileId { get; set; }
    }
}

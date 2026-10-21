using SchoolNewspaperBlazorApp.Data;

namespace SchoolNewspaperBlazorApp.Interfaces.Repository
{
    public interface IArticleRepository
    {
        Task AddArticleAsync(Article article);
        Task<List<Article>> GetAllArticlesAsync();
        Task<Article> GetArticleByIdAsync(int id);
        Task UpdateArticleAsync(Article article);
        // Usuwa artykuł. Zwraca Id jego zdjęcia, jeśli po usunięciu żaden inny artykuł już go nie używa
        // (wtedy zdjęcie można skasować), w przeciwnym razie null.
        Task<int?> RemoveArticleByIdAsync(int id);
    }
}

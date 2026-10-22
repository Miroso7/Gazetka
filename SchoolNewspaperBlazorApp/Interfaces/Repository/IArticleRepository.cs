using SchoolNewspaperBlazorApp.Data;

namespace SchoolNewspaperBlazorApp.Interfaces.Repository
{
    public interface IArticleRepository
    {
        Task AddArticleAsync(Article article);
        Task<List<Article>> GetAllArticlesAsync();
        Task<Article> GetArticleByIdAsync(int id);
        // Aktualizuje artykuł oraz jego galerię: dodaje zdjęcia o podanych FileId i usuwa wpisy o podanych Id.
        // Zwraca FileId usuniętych zdjęć (same pliki kasuje serwis).
        Task<List<int>> UpdateArticleAsync(Article article, IReadOnlyCollection<int> addedFileIds, IReadOnlyCollection<int> removedImageIds);
        // Usuwa artykuł. Zwraca Id zdjęć (głównego i z galerii), których po usunięciu nie używa już nic innego
        // (wtedy pliki można skasować).
        Task<List<int>> RemoveArticleByIdAsync(int id);
        Task<List<ArticleImageDto>> GetArticleImagesAsync(int articleId);
        Task<int> CountArticleImagesAsync(int articleId);
    }
}

using SchoolNewspaperBlazorApp.Data;
using SchoolNewspaperBlazorApp.Data.Enums;

namespace SchoolNewspaperBlazorApp.Interfaces.Repository
{
    public interface IArticleRepository
    {
        Task AddArticleAsync(Article article);
        Task<List<Article>> GetAllArticlesAsync();
        Task<List<Article>> GetArticlesByTypeAsync(ArticleType type);
        // Tylko artykuły widoczne dla czytelników (opublikowane i niewygasłe), przypięte na górze; type = null oznacza wszystkie kategorie.
        Task<List<Article>> GetPublishedArticlesAsync(ArticleType? type);
        Task<Article> GetArticleByIdAsync(int id);
        // Aktualizuje artykuł oraz jego galerię: dodaje zdjęcia o podanych FileId i usuwa wpisy o podanych Id.
        // addedPlacements: ułożenie dodawanych zdjęć (w tej samej kolejności); changedPlacements: nowe ułożenie zdjęć już w galerii (Id wpisu -> ułożenie).
        // Zwraca FileId usuniętych zdjęć (wpisy o plikach oznacza jako usunięte serwis).
        Task<List<int>> UpdateArticleAsync(Article article, IReadOnlyCollection<int> addedFileIds, IReadOnlyCollection<int> removedImageIds,
            IReadOnlyList<ArticleImagePlacement>? addedPlacements = null, IReadOnlyDictionary<int, ArticleImagePlacement>? changedPlacements = null);
        // Usuwa artykuł miękko (IsDeleted = 1). Zdjęcia i galeria zostają, żeby artykuł dało się przywrócić.
        Task RemoveArticleByIdAsync(int id);
        Task<List<ArticleImageDto>> GetArticleImagesAsync(int articleId);
        Task<int> CountArticleImagesAsync(int articleId);
    }
}

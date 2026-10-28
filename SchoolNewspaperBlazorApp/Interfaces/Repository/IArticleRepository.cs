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
        // Tworzy wersję roboczą (Id == 0) albo aktualizuje istniejącą; zapisuje tylko pola tekstowe i ustawienia publikacji. Zwraca jej Id.
        Task<int> SaveDraftAsync(Article draft);
        // Usunięte artykuły (kosz), od ostatnio usuniętych.
        Task<List<Article>> GetDeletedArticlesAsync();
        // Przywraca usunięty artykuł (zdjęcia i galeria nigdy nie były kasowane); false, gdy nie ma takiego w koszu.
        Task<bool> RestoreArticleAsync(int id);
        // Trwale usuwa artykuł z kosza razem z wpisami galerii (DELETE w bazie, bez powrotu). Zwraca FileId jego zdjęć
        // (główne i z galerii) albo null, gdy artykułu nie ma w koszu. Wpisy o plikach oznacza jako usunięte serwis.
        Task<List<int>?> PurgeArticleAsync(int id);
        // Zwiększa licznik wyświetleń opublikowanego artykułu o 1.
        Task IncrementViewCountAsync(int id);
        Task<List<ArticleImageDto>> GetArticleImagesAsync(int articleId);
        Task<int> CountArticleImagesAsync(int articleId);
    }
}

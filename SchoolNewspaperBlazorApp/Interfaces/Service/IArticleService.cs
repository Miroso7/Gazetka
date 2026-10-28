using SchoolNewspaperBlazorApp.Data;
using SchoolNewspaperBlazorApp.Data.Enums;

namespace SchoolNewspaperBlazorApp.Interfaces.Service
{
    public interface IArticleService
    {
        // Maksymalna liczba zdjęć w galerii jednego artykułu (nie licząc głównego zdjęcia).
        const int MaxGalleryImages = 12;

        // details: dodatkowe pola zależne od typu artykułu (klucze z ArticleFields.For(type))
        // galleryFileIds: Id wgranych wcześniej zdjęć, które trafiają do galerii artykułu
        // galleryPlacements: ułożenie tych zdjęć w treści (w tej samej kolejności co galleryFileIds; null = wszystkie tylko w galerii)
        // isPinned: artykuł zawsze na górze listy; publishAt: „Opublikuj od” (null = od razu); expiresAt: „Wyświetlaj do” (null = bez końca)
        Task AddArticleAsync(string title, string text, string author, ArticleType type, int id, IReadOnlyList<int>? galleryFileIds = null,
            IReadOnlyDictionary<string, string>? details = null, bool isPinned = false, DateTime? publishAt = null, DateTime? expiresAt = null,
            IReadOnlyList<ArticleImagePlacement>? galleryPlacements = null);
        // Wszystkie artykuły (także zaplanowane i wygasłe) – do panelu redakcji.
        Task<List<Article>> GetAllArticlesAsync();
        // Artykuły widoczne dla czytelników: opublikowane i niewygasłe, przypięte na górze; type = null oznacza wszystkie kategorie.
        Task<List<Article>> GetPublishedArticlesAsync(ArticleType? type = null);
        // artykuły jednego typu (osobna zakładka), od najnowszych
        Task<List<Article>> GetArticlesByTypeAsync(ArticleType type);
        // Artykuł pod bezpośrednim linkiem. Zaplanowany (jeszcze nieopublikowany) widzi tylko redakcja, dla pozostałych zwraca null;
        // wygasły otwiera się normalnie.
        Task<Article?> GetArticleByIdAsync(int id);
        Task<List<ArticleImageDto>> GetArticleImagesAsync(int articleId);
        // addedFileIds: nowe zdjęcia do galerii (addedPlacements: ich ułożenie, w tej samej kolejności); removedImageIds: Id wpisów galerii (ArticleImageDto.Id) do usunięcia
        // changedPlacements: nowe ułożenie zdjęć już zapisanych w galerii (Id wpisu galerii -> ułożenie)
        // publishAt = null zostawia dotychczasową datę publikacji (a gdy jest w przyszłości, publikuje artykuł od razu)
        Task UpdateArticleAsync(int id, string title, string text, string author, ArticleType type, int fileId,
            IReadOnlyList<int>? addedFileIds = null, IReadOnlyList<int>? removedImageIds = null,
            IReadOnlyDictionary<string, string>? details = null, bool isPinned = false, DateTime? publishAt = null, DateTime? expiresAt = null,
            IReadOnlyList<ArticleImagePlacement>? addedPlacements = null, IReadOnlyDictionary<int, ArticleImagePlacement>? changedPlacements = null);
        Task RemoveArticleByIdAsync(int id);
    }
}

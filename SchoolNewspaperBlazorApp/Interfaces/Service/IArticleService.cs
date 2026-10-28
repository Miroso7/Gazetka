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
        // isPinned: artykuł zawsze na górze listy; publishAt: „Opublikuj od” (null = od razu); expiresAt: „Wyświetlaj do” (null = bez końca)
        Task AddArticleAsync(string title, string text, string author, ArticleType type, int id, IReadOnlyList<int>? galleryFileIds = null,
            IReadOnlyDictionary<string, string>? details = null, bool isPinned = false, DateTime? publishAt = null, DateTime? expiresAt = null);
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
        // addedFileIds: nowe zdjęcia do galerii; removedImageIds: Id wpisów galerii (ArticleImageDto.Id) do usunięcia
        // publishAt = null zostawia dotychczasową datę publikacji (a gdy jest w przyszłości, publikuje artykuł od razu)
        Task UpdateArticleAsync(int id, string title, string text, string author, ArticleType type, int fileId,
            IReadOnlyList<int>? addedFileIds = null, IReadOnlyList<int>? removedImageIds = null,
            IReadOnlyDictionary<string, string>? details = null, bool isPinned = false, DateTime? publishAt = null, DateTime? expiresAt = null);
        Task RemoveArticleByIdAsync(int id);
    }
}

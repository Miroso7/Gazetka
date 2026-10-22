using SchoolNewspaperBlazorApp.Data;

namespace SchoolNewspaperBlazorApp.Interfaces.Service
{
    public interface IArticleService
    {
        // Maksymalna liczba zdjęć w galerii jednego artykułu (nie licząc głównego zdjęcia).
        const int MaxGalleryImages = 12;

        // galleryFileIds: Id wgranych wcześniej zdjęć, które trafiają do galerii artykułu
        Task AddArticleAsync(string title, string text, string author, int id, IReadOnlyList<int>? galleryFileIds = null);
        Task<List<Article>> GetAllArticlesAsync();
        Task<Article> GetArticleByIdAsync(int id);
        Task<List<ArticleImageDto>> GetArticleImagesAsync(int articleId);
        // addedFileIds: nowe zdjęcia do galerii; removedImageIds: Id wpisów galerii (ArticleImageDto.Id) do usunięcia
        Task UpdateArticleAsync(int id, string title, string text, string author, int fileId,
            IReadOnlyList<int>? addedFileIds = null, IReadOnlyList<int>? removedImageIds = null);
        Task RemoveArticleByIdAsync(int id);
    }
}

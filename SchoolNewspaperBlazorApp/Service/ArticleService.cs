using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using SchoolNewspaperBlazorApp.Data;
using SchoolNewspaperBlazorApp.Data.Enums;
using SchoolNewspaperBlazorApp.Interfaces.Repository;
using SchoolNewspaperBlazorApp.Interfaces.Service;

namespace SchoolNewspaperBlazorApp.Service
{
    public class ArticleService : IArticleService
    {
        private readonly IArticleRepository _articleRepository;
        private readonly IPermissionService _permissionService;
        private readonly IFileService _fileService;
        private readonly ILogger<ArticleService> _logger;
        public ArticleService(
            IArticleRepository articleRepository,
            IPermissionService permissionService,
            IFileService fileService,
            ILogger<ArticleService> logger)
        {
            _articleRepository = articleRepository;
            _permissionService = permissionService;
            _fileService = fileService;
            _logger = logger;
        }
        public async Task AddArticleAsync(string title, string text, string author, ArticleType type, int fileId, IReadOnlyList<int>? galleryFileIds = null,
            IReadOnlyDictionary<string, string>? details = null)
        {
            await _permissionService.RequirePermissionAsync(EnumPermissions.AddArticle);
            ValidateArticleFields(title, text);
            ValidateType(type);
            var detailsJson = ArticleFields.ToJson(type, details);
            var gallery = galleryFileIds ?? Array.Empty<int>();
            ValidateGalleryCount(gallery.Count);
            var article = new Article
            {
                Title = title.Trim(),
                Text = text.Trim(),
                Author = author?.Trim() ?? string.Empty,
                PublishDate = DateTime.Now,
                Type = type,
                Details = detailsJson,
                FileId = fileId,
                // artykuł i jego galeria zapisują się razem, w jednej transakcji
                Images = gallery.Select(galleryFileId => new ArticleImage { FileId = galleryFileId }).ToList()
            };
            await _articleRepository.AddArticleAsync(article);
            _logger.LogInformation("Dodano artykuł {ArticleId} (typ {Type}, zdjęcia w galerii: {GalleryCount}).", article.Id, type, gallery.Count);
        }
        public async Task<List<Article>> GetAllArticlesAsync()
        {
            return await _articleRepository.GetAllArticlesAsync();
        }
        public async Task<List<Article>> GetArticlesByTypeAsync(ArticleType type)
        {
            return await _articleRepository.GetArticlesByTypeAsync(type);
        }
        public async Task<Article> GetArticleByIdAsync(int id)
        {
            Article article = await _articleRepository.GetArticleByIdAsync(id);
            if (article == null)
            {
                _logger.LogWarning("Nie znaleziono artykułu {ArticleId}.", id);
                throw new Exception($"Article with ID {id} not found.");
            }
            return article;
        }
        public async Task<List<ArticleImageDto>> GetArticleImagesAsync(int articleId)
        {
            return await _articleRepository.GetArticleImagesAsync(articleId);
        }
        public async Task UpdateArticleAsync(int id, string title, string text, string author, ArticleType type, int fileId,
            IReadOnlyList<int>? addedFileIds = null, IReadOnlyList<int>? removedImageIds = null,
            IReadOnlyDictionary<string, string>? details = null)
        {
            await _permissionService.RequirePermissionAsync(EnumPermissions.EditArticle);
            if (id <= 0)
            {
                throw new ArgumentException("Nieprawidłowe ID artykułu.", nameof(id));
            }
            ValidateArticleFields(title, text);
            ValidateType(type);
            var detailsJson = ArticleFields.ToJson(type, details);

            var added = addedFileIds ?? Array.Empty<int>();
            var removed = removedImageIds ?? Array.Empty<int>();
            if (added.Count > 0)
            {
                var current = await _articleRepository.CountArticleImagesAsync(id);
                ValidateGalleryCount(Math.Max(0, current - removed.Count) + added.Count);
            }

            // data publikacji zostaje bez zmian (repozytorium aktualizuje tylko pola poniżej)
            var article = new Article
            {
                Id = id,
                Title = title.Trim(),
                Text = text.Trim(),
                Author = author?.Trim() ?? string.Empty,
                Type = type,
                Details = detailsJson,
                FileId = fileId
            };
            var removedFileIds = await _articleRepository.UpdateArticleAsync(article, added, removed);
            _logger.LogInformation("Zaktualizowano artykuł {ArticleId} (dodane zdjęcia: {Added}, usunięte: {Removed}).", id, added.Count, removed.Count);

            // wpisy w bazie są już zaktualizowane, więc błąd przy kasowaniu plików nie może pokazać błędu
            if (removedFileIds.Count > 0)
            {
                try
                {
                    await _fileService.DeleteFilesAsync(removedFileIds);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Nie udało się usunąć zdjęć galerii artykułu {ArticleId}.", id);
                }
            }
        }
        // Tytuł i treść są wymagane, autor jest opcjonalny.
        private static void ValidateArticleFields(string? title, string? text)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new InvalidOperationException("Podaj tytuł artykułu.");
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                throw new InvalidOperationException("Podaj treść artykułu.");
            }
        }
        private static void ValidateType(ArticleType type)
        {
            if (!Enum.IsDefined(type))
            {
                throw new InvalidOperationException("Wybierz kategorię artykułu.");
            }
        }
        private static void ValidateGalleryCount(int count)
        {
            if (count > IArticleService.MaxGalleryImages)
            {
                throw new InvalidOperationException(
                    $"Galeria może mieć maksymalnie {IArticleService.MaxGalleryImages} zdjęć.");
            }
        }
        public async Task RemoveArticleByIdAsync(int id)
        {
            await _permissionService.RequirePermissionAsync(EnumPermissions.DeleteArticle);
            if (id <= 0) return;

            // soft delete: artykuł znika z serwisu, ale jego zdjęcia zostają (przywrócenie = zmiana flagi w bazie)
            await _articleRepository.RemoveArticleByIdAsync(id);
            _logger.LogInformation("Usunięto artykuł {ArticleId} (soft delete).", id);
        }
    }
}

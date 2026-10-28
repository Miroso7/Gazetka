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
            IReadOnlyDictionary<string, string>? details = null, bool isPinned = false, DateTime? publishAt = null, DateTime? expiresAt = null,
            IReadOnlyList<ArticleImagePlacement>? galleryPlacements = null)
        {
            await _permissionService.RequirePermissionAsync(EnumPermissions.AddArticle);
            ValidateArticleFields(title, text);
            ValidateType(type);
            var publishDate = publishAt ?? DateTime.Now;
            ValidateSchedule(publishDate, expiresAt);
            var detailsJson = ArticleFields.ToJson(type, details);
            var gallery = galleryFileIds ?? Array.Empty<int>();
            ValidateGalleryCount(gallery.Count);
            var article = new Article
            {
                Title = title.Trim(),
                Text = text.Trim(),
                Author = author?.Trim() ?? string.Empty,
                PublishDate = publishDate,
                ExpiresAt = expiresAt,
                IsPinned = isPinned,
                Type = type,
                Details = detailsJson,
                FileId = fileId,
                // artykuł i jego galeria zapisują się razem, w jednej transakcji
                Images = gallery.Select((galleryFileId, index) =>
                {
                    var placement = PlacementAt(galleryPlacements, index);
                    return new ArticleImage { FileId = galleryFileId, Placement = placement.Placement, InlineAfter = placement.After };
                }).ToList()
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
        public async Task<List<Article>> GetPublishedArticlesAsync(ArticleType? type = null)
        {
            return await _articleRepository.GetPublishedArticlesAsync(type);
        }
        public async Task<Article?> GetArticleByIdAsync(int id)
        {
            Article article = await _articleRepository.GetArticleByIdAsync(id);
            if (article == null)
            {
                _logger.LogWarning("Nie znaleziono artykułu {ArticleId}.", id);
                throw new Exception($"Article with ID {id} not found.");
            }

            // zaplanowany artykuł pod bezpośrednim linkiem widzi tylko redakcja (wygasły otwiera się normalnie)
            if (article.PublishDate > DateTime.Now && !await CanSeeUnpublishedAsync())
            {
                _logger.LogDebug("Artykuł {ArticleId} jest zaplanowany, a użytkownik nie należy do redakcji.", id);
                return null;
            }
            return article;
        }
        // Redakcja: każdy, kto może dodawać, edytować lub usuwać artykuły (albo jest masterem).
        private async Task<bool> CanSeeUnpublishedAsync()
        {
            var mine = await _permissionService.GetMyPermissionsAsync();
            return mine.Contains(EnumPermissions.AddArticle)
                || mine.Contains(EnumPermissions.EditArticle)
                || mine.Contains(EnumPermissions.DeleteArticle)
                || mine.Contains(EnumPermissions.Master);
        }
        public async Task<List<ArticleImageDto>> GetArticleImagesAsync(int articleId)
        {
            return await _articleRepository.GetArticleImagesAsync(articleId);
        }
        public async Task UpdateArticleAsync(int id, string title, string text, string author, ArticleType type, int fileId,
            IReadOnlyList<int>? addedFileIds = null, IReadOnlyList<int>? removedImageIds = null,
            IReadOnlyDictionary<string, string>? details = null, bool isPinned = false, DateTime? publishAt = null, DateTime? expiresAt = null,
            IReadOnlyList<ArticleImagePlacement>? addedPlacements = null, IReadOnlyDictionary<int, ArticleImagePlacement>? changedPlacements = null)
        {
            await _permissionService.RequirePermissionAsync(EnumPermissions.EditArticle);
            if (id <= 0)
            {
                throw new ArgumentException("Nieprawidłowe ID artykułu.", nameof(id));
            }
            ValidateArticleFields(title, text);
            ValidateType(type);

            // brak daty „Opublikuj od” zostawia dotychczasową datę publikacji; zaplanowany artykuł z wyczyszczoną datą publikuje się od razu
            var existing = await _articleRepository.GetArticleByIdAsync(id);
            var now = DateTime.Now;
            var publishDate = publishAt ?? (existing.PublishDate > now ? now : existing.PublishDate);
            ValidateSchedule(publishDate, expiresAt);
            var detailsJson = ArticleFields.ToJson(type, details);

            var added = addedFileIds ?? Array.Empty<int>();
            var removed = removedImageIds ?? Array.Empty<int>();
            if (added.Count > 0)
            {
                var current = await _articleRepository.CountArticleImagesAsync(id);
                ValidateGalleryCount(Math.Max(0, current - removed.Count) + added.Count);
            }

            // repozytorium aktualizuje tylko pola poniżej
            var article = new Article
            {
                Id = id,
                Title = title.Trim(),
                Text = text.Trim(),
                Author = author?.Trim() ?? string.Empty,
                PublishDate = publishDate,
                ExpiresAt = expiresAt,
                IsPinned = isPinned,
                Type = type,
                Details = detailsJson,
                FileId = fileId
            };
            var cleanAdded = added.Select((_, index) => PlacementAt(addedPlacements, index)).ToList();
            var cleanChanged = changedPlacements?.ToDictionary(pair => pair.Key, pair => Sanitize(pair.Value));
            var removedFileIds = await _articleRepository.UpdateArticleAsync(article, added, removed, cleanAdded, cleanChanged);
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
        // brak ułożenia = zdjęcie tylko w galerii; nieprawidłowe wartości też wracają do galerii
        private static ArticleImagePlacement PlacementAt(IReadOnlyList<ArticleImagePlacement>? placements, int index) =>
            placements is not null && index < placements.Count ? Sanitize(placements[index]) : new ArticleImagePlacement(InlinePlacement.Gallery, 0);

        private static ArticleImagePlacement Sanitize(ArticleImagePlacement value) =>
            Enum.IsDefined(value.Placement)
                ? new ArticleImagePlacement(value.Placement, Math.Max(0, value.After))
                : new ArticleImagePlacement(InlinePlacement.Gallery, 0);
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
        // „Wyświetlaj do” musi wypadać po „Opublikuj od”, inaczej artykuł nigdy nie byłby widoczny.
        private static void ValidateSchedule(DateTime publishDate, DateTime? expiresAt)
        {
            if (expiresAt is { } end && end <= publishDate)
            {
                throw new InvalidOperationException("Data „Wyświetlaj do” musi być późniejsza niż „Opublikuj od”.");
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

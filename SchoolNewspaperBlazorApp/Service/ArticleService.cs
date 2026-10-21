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
        public async Task AddArticleAsync(string title, string text, string author, int fileId)
        {
            await _permissionService.RequirePermissionAsync(EnumPermissions.AddArticle);
            ValidateArticleFields(title, text);
            var article = new Article
            {
                Title = title.Trim(),
                Text = text.Trim(),
                Author = author?.Trim() ?? string.Empty,
                PublishDate = DateTime.Now,
                FileId = fileId
            };
            await _articleRepository.AddArticleAsync(article);
        }
        public async Task<List<Article>> GetAllArticlesAsync()
        {
            return await _articleRepository.GetAllArticlesAsync();
        }
        public async Task<Article> GetArticleByIdAsync(int id)
        {
            Article article = await _articleRepository.GetArticleByIdAsync(id);
            if (article == null)
            {
                throw new Exception($"Article with ID {id} not found.");
            }
            return article;
        }
        public async Task UpdateArticleAsync(int id, string title, string text, string author, int fileId)
        {
            await _permissionService.RequirePermissionAsync(EnumPermissions.EditArticle);
            if (id <= 0)
            {
                throw new ArgumentException("Nieprawidłowe ID artykułu.", nameof(id));
            }
            ValidateArticleFields(title, text);

            // data publikacji zostaje bez zmian (repozytorium aktualizuje tylko pola poniżej)
            var article = new Article
            {
                Id = id,
                Title = title.Trim(),
                Text = text.Trim(),
                Author = author?.Trim() ?? string.Empty,
                FileId = fileId
            };
            await _articleRepository.UpdateArticleAsync(article);
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
        public async Task RemoveArticleByIdAsync(int id)
        {
            await _permissionService.RequirePermissionAsync(EnumPermissions.DeleteArticle);
            if (id <= 0) return;

            var orphanedFileId = await _articleRepository.RemoveArticleByIdAsync(id);
            if (orphanedFileId is null) return;

            // artykuł jest już usunięty, więc błąd przy kasowaniu zdjęcia nie może go "cofnąć" ani pokazać błędu
            try
            {
                await _fileService.DeleteFileAsync(orphanedFileId.Value);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Nie udało się usunąć zdjęcia (FileId {FileId}) po usunięciu artykułu {ArticleId}.", orphanedFileId, id);
            }
        }
    }
}

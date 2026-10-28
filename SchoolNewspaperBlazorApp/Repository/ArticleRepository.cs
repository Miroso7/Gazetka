using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SchoolNewspaperBlazorApp.Data;
using SchoolNewspaperBlazorApp.Data.Enums;
using SchoolNewspaperBlazorApp.Interfaces.Repository;

namespace SchoolNewspaperBlazorApp.Repository
{
    public class ArticleRepository : IArticleRepository
    {
        private readonly IDbContextFactory<NewspaperDbContext> _contextFactory;
        private readonly ILogger<ArticleRepository> _logger;

        public ArticleRepository(IDbContextFactory<NewspaperDbContext> contextFactory, ILogger<ArticleRepository> logger)
        {
            _contextFactory = contextFactory;
            _logger = logger;
        }

        public async Task AddArticleAsync(Article article)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            await context.Articles.AddAsync(article);
            await context.SaveChangesAsync();
            _logger.LogDebug("Zapisano artykuł {ArticleId} w bazie.", article.Id);
        }

        public async Task<List<Article>> GetAllArticlesAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            // najnowsze artykuły na górze (spójnie z panelem admina i odznaką „Nowe”)
            return await context.Articles
                .AsNoTracking()
                .OrderByDescending(a => a.Id)
                .ToListAsync();
        }

        public async Task<List<Article>> GetPublishedArticlesAsync(ArticleType? type)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var now = DateTime.Now;
            var query = context.Articles
                .AsNoTracking()
                .Where(a => !a.IsDraft && a.PublishDate <= now && (a.ExpiresAt == null || a.ExpiresAt > now));

            if (type is { } t)
            {
                query = query.Where(a => a.Type == t);
            }

            // przypięte na górze, w obrębie grup od najnowszych
            return await query
                .OrderByDescending(a => a.IsPinned)
                .ThenByDescending(a => a.PublishDate)
                .ThenByDescending(a => a.Id)
                .ToListAsync();
        }

        public async Task<List<Article>> GetArticlesByTypeAsync(ArticleType type)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await context.Articles
                .AsNoTracking()
                .Where(a => !a.IsDraft && a.Type == type)
                .OrderByDescending(a => a.Id)
                .ToListAsync();
        }

        public async Task<Article> GetArticleByIdAsync(int id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var article = await context.Articles.FirstOrDefaultAsync(a => a.Id == id);
            if (article == null)
            {
                _logger.LogWarning("Nie znaleziono artykułu {ArticleId}.", id);
                throw new Exception($"Article with ID {id} not found.");
            }
            return article;
        }

        public async Task<List<int>> UpdateArticleAsync(Article article, IReadOnlyCollection<int> addedFileIds, IReadOnlyCollection<int> removedImageIds,
            IReadOnlyList<ArticleImagePlacement>? addedPlacements = null, IReadOnlyDictionary<int, ArticleImagePlacement>? changedPlacements = null)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var existing = await context.Articles.FirstOrDefaultAsync(a => a.Id == article.Id);
            if (existing == null)
            {
                _logger.LogWarning("Nie można zaktualizować artykułu {ArticleId}: nie istnieje.", article.Id);
                throw new Exception($"Article with ID {article.Id} not found.");
            }

            // aktualizujemy tylko edytowalne pola
            existing.Title = article.Title;
            existing.Text = article.Text;
            existing.Author = article.Author;
            existing.Type = article.Type;
            existing.Details = article.Details;
            existing.FileId = article.FileId;
            existing.ImageFocusX = article.ImageFocusX;
            existing.ImageFocusY = article.ImageFocusY;
            existing.PublishDate = article.PublishDate;
            existing.ExpiresAt = article.ExpiresAt;
            existing.IsPinned = article.IsPinned;
            // pełny zapis formularza publikuje też wersję roboczą
            existing.IsDraft = false;

            // galeria: usuwamy (soft delete) tylko zdjęcia tego artykułu
            var removedFileIds = new List<int>();
            var removedIds = removedImageIds.ToList();
            if (removedIds.Count > 0)
            {
                var toRemove = await context.ArticleImages
                    .Where(i => i.ArticleId == article.Id && removedIds.Contains(i.Id))
                    .ToListAsync();
                removedFileIds.AddRange(toRemove.Select(i => i.FileId));
                context.ArticleImages.RemoveRange(toRemove);
            }

            // ułożenie zdjęć, które zostają w galerii
            if (changedPlacements is { Count: > 0 })
            {
                var changedIds = changedPlacements.Keys.ToList();
                var kept = await context.ArticleImages
                    .Where(i => i.ArticleId == article.Id && changedIds.Contains(i.Id))
                    .ToListAsync();
                foreach (var image in kept)
                {
                    var placement = changedPlacements[image.Id];
                    image.Placement = placement.Placement;
                    image.InlineAfter = placement.After;
                    image.SortOrder = placement.Order;
                }
            }

            var addedList = addedFileIds.ToList();
            for (var index = 0; index < addedList.Count; index++)
            {
                var placement = addedPlacements is not null && index < addedPlacements.Count
                    ? addedPlacements[index]
                    : new ArticleImagePlacement(InlinePlacement.Gallery, 0);
                context.ArticleImages.Add(new ArticleImage
                {
                    ArticleId = article.Id,
                    FileId = addedList[index],
                    Placement = placement.Placement,
                    InlineAfter = placement.After,
                    SortOrder = placement.Order
                });
            }

            await context.SaveChangesAsync();
            _logger.LogDebug("Zaktualizowano artykuł {ArticleId} (dodane zdjęcia: {Added}, usunięte: {Removed}).",
                article.Id, addedFileIds.Count, removedFileIds.Count);
            return removedFileIds;
        }

        public async Task<int> SaveDraftAsync(Article draft)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            if (draft.Id == 0)
            {
                draft.IsDraft = true;
                await context.Articles.AddAsync(draft);
                await context.SaveChangesAsync();
                _logger.LogDebug("Utworzono wersję roboczą artykułu {ArticleId}.", draft.Id);
                return draft.Id;
            }

            // nadpisujemy tylko wersje robocze: opublikowany artykuł nie może zmienić się przez autozapis
            var existing = await context.Articles.FirstOrDefaultAsync(a => a.Id == draft.Id && a.IsDraft);
            if (existing == null)
            {
                _logger.LogWarning("Nie można zapisać wersji roboczej {ArticleId}: nie istnieje albo jest już opublikowana.", draft.Id);
                throw new InvalidOperationException("Ta wersja robocza nie istnieje albo została już opublikowana.");
            }

            existing.Title = draft.Title;
            existing.Text = draft.Text;
            existing.Author = draft.Author;
            existing.Type = draft.Type;
            existing.Details = draft.Details;
            existing.PublishDate = draft.PublishDate;
            existing.ExpiresAt = draft.ExpiresAt;
            existing.IsPinned = draft.IsPinned;
            await context.SaveChangesAsync();
            return existing.Id;
        }

        public async Task<List<Article>> GetDeletedArticlesAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await context.Articles
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(a => a.IsDeleted)
                .OrderByDescending(a => a.DeletedAt)
                .ThenByDescending(a => a.Id)
                .ToListAsync();
        }

        public async Task<bool> RestoreArticleAsync(int id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var article = await context.Articles
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(a => a.Id == id && a.IsDeleted);
            if (article == null)
            {
                _logger.LogWarning("Nie można przywrócić artykułu {ArticleId}: nie ma go w koszu.", id);
                return false;
            }

            article.IsDeleted = false;
            article.DeletedAt = null;
            await context.SaveChangesAsync();
            _logger.LogDebug("Przywrócono artykuł {ArticleId} z kosza.", id);
            return true;
        }

        public async Task<List<int>?> PurgeArticleAsync(int id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            // tylko artykuły z kosza: nie da się w ten sposób trwale usunąć opublikowanego artykułu
            var mainFileId = await context.Articles
                .IgnoreQueryFilters()
                .Where(a => a.Id == id && a.IsDeleted)
                .Select(a => (int?)a.FileId)
                .FirstOrDefaultAsync();
            if (mainFileId is null)
            {
                _logger.LogWarning("Nie można trwale usunąć artykułu {ArticleId}: nie ma go w koszu.", id);
                return null;
            }

            var fileIds = await context.ArticleImages
                .IgnoreQueryFilters()
                .Where(i => i.ArticleId == id)
                .Select(i => i.FileId)
                .ToListAsync();
            fileIds.Add(mainFileId.Value);

            // ExecuteDelete omija soft delete z SaveChanges: to prawdziwy DELETE, najpierw galeria, potem artykuł (jedna transakcja)
            await using var transaction = await context.Database.BeginTransactionAsync();
            await context.ArticleImages.IgnoreQueryFilters().Where(i => i.ArticleId == id).ExecuteDeleteAsync();
            await context.Articles.IgnoreQueryFilters().Where(a => a.Id == id && a.IsDeleted).ExecuteDeleteAsync();
            await transaction.CommitAsync();

            _logger.LogDebug("Trwale usunięto artykuł {ArticleId}.", id);
            return fileIds.Where(f => f > 0).Distinct().ToList();
        }

        public async Task IncrementViewCountAsync(int id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var now = DateTime.Now;
            await context.Articles
                .Where(a => a.Id == id && !a.IsDraft && a.PublishDate <= now)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.ViewCount, a => a.ViewCount + 1));
        }

        public async Task<List<ArticleImageDto>> GetArticleImagesAsync(int articleId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await context.ArticleImages
                .AsNoTracking()
                .Where(i => i.ArticleId == articleId)
                .Join(context.Files, i => i.FileId, f => f.Id,
                    (i, f) => new ArticleImageDto { Id = i.Id, FileId = i.FileId, FileName = f.FileName, Placement = i.Placement, InlineAfter = i.InlineAfter, SortOrder = i.SortOrder })
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Id)
                .ToListAsync();
        }

        public async Task<int> CountArticleImagesAsync(int articleId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await context.ArticleImages.CountAsync(i => i.ArticleId == articleId);
        }

        public async Task RemoveArticleByIdAsync(int id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var article = await context.Articles.FirstOrDefaultAsync(a => a.Id == id);
            if (article == null)
            {
                _logger.LogWarning("Nie można usunąć artykułu {ArticleId}: nie istnieje.", id);
                throw new Exception($"Article with ID {id} not found.");
            }

            // Remove() zamienia się na soft delete (UPDATE IsDeleted = 1, patrz NewspaperDbContext);
            // galeria i zdjęcia zostają nietknięte, więc przywrócenie artykułu to jedna zmiana flagi
            context.Articles.Remove(article);
            await context.SaveChangesAsync();
            _logger.LogDebug("Oznaczono artykuł {ArticleId} jako usunięty.", id);
        }
    }
}

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

        public async Task<List<Article>> GetArticlesByTypeAsync(ArticleType type)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await context.Articles
                .AsNoTracking()
                .Where(a => a.Type == type)
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

        public async Task<List<int>> UpdateArticleAsync(Article article, IReadOnlyCollection<int> addedFileIds, IReadOnlyCollection<int> removedImageIds)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var existing = await context.Articles.FirstOrDefaultAsync(a => a.Id == article.Id);
            if (existing == null)
            {
                _logger.LogWarning("Nie można zaktualizować artykułu {ArticleId}: nie istnieje.", article.Id);
                throw new Exception($"Article with ID {article.Id} not found.");
            }

            // aktualizujemy tylko edytowalne pola (data publikacji zostaje)
            existing.Title = article.Title;
            existing.Text = article.Text;
            existing.Author = article.Author;
            existing.Type = article.Type;
            existing.Details = article.Details;
            existing.FileId = article.FileId;

            // galeria: usuwamy tylko zdjęcia tego artykułu
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

            foreach (var fileId in addedFileIds)
            {
                context.ArticleImages.Add(new ArticleImage { ArticleId = article.Id, FileId = fileId });
            }

            await context.SaveChangesAsync();
            _logger.LogDebug("Zaktualizowano artykuł {ArticleId} (dodane zdjęcia: {Added}, usunięte: {Removed}).",
                article.Id, addedFileIds.Count, removedFileIds.Count);
            return removedFileIds;
        }

        public async Task<List<ArticleImageDto>> GetArticleImagesAsync(int articleId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await context.ArticleImages
                .AsNoTracking()
                .Where(i => i.ArticleId == articleId)
                .Join(context.Files, i => i.FileId, f => f.Id,
                    (i, f) => new ArticleImageDto { Id = i.Id, FileId = i.FileId, FileName = f.FileName })
                .OrderBy(x => x.Id)
                .ToListAsync();
        }

        public async Task<int> CountArticleImagesAsync(int articleId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await context.ArticleImages.CountAsync(i => i.ArticleId == articleId);
        }

        public async Task<List<int>> RemoveArticleByIdAsync(int id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var article = await context.Articles.FirstOrDefaultAsync(a => a.Id == id);
            if (article == null)
            {
                _logger.LogWarning("Nie można usunąć artykułu {ArticleId}: nie istnieje.", id);
                throw new Exception($"Article with ID {id} not found.");
            }

            var coverFileId = article.FileId;
            var galleryFileIds = await context.ArticleImages
                .Where(i => i.ArticleId == id)
                .Select(i => i.FileId)
                .ToListAsync();

            // wpisy o zdjęciach z galerii usuwa baza (kaskadowo)
            context.Articles.Remove(article);
            await context.SaveChangesAsync();

            // zdjęcie do skasowania tylko wtedy, gdy nie używa go już żaden inny artykuł ani galeria
            var orphaned = new List<int>();
            foreach (var fileId in galleryFileIds.Append(coverFileId).Distinct())
            {
                if (fileId <= 0) continue;
                if (await context.Articles.AnyAsync(a => a.FileId == fileId)) continue;
                if (await context.ArticleImages.AnyAsync(i => i.FileId == fileId)) continue;

                orphaned.Add(fileId);
            }
            _logger.LogDebug("Usunięto artykuł {ArticleId} z bazy (zdjęcia do skasowania: {Orphaned}).", id, orphaned.Count);
            return orphaned;
        }
    }
}

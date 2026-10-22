using Microsoft.EntityFrameworkCore;
using SchoolNewspaperBlazorApp.Data;
using SchoolNewspaperBlazorApp.Interfaces.Repository;

namespace SchoolNewspaperBlazorApp.Repository
{
    public class ArticleRepository : IArticleRepository
    {
        private readonly IDbContextFactory<NewspaperDbContext> _contextFactory;

        public ArticleRepository(IDbContextFactory<NewspaperDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task AddArticleAsync(Article article)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            await context.Articles.AddAsync(article);
            await context.SaveChangesAsync();
        }

        public async Task<List<Article>> GetAllArticlesAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await context.Articles.ToListAsync();
        }

        public async Task<Article> GetArticleByIdAsync(int id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var article = await context.Articles.FirstOrDefaultAsync(a => a.Id == id);
            if (article == null)
            {
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
                throw new Exception($"Article with ID {article.Id} not found.");
            }

            // aktualizujemy tylko edytowalne pola (data publikacji zostaje)
            existing.Title = article.Title;
            existing.Text = article.Text;
            existing.Author = article.Author;
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
            return orphaned;
        }
    }
}

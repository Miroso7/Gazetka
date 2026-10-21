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

        public async Task UpdateArticleAsync(Article article)
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

            await context.SaveChangesAsync();
        }

        public async Task<int?> RemoveArticleByIdAsync(int id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var article = await context.Articles.FirstOrDefaultAsync(a => a.Id == id);
            if (article == null)
            {
                throw new Exception($"Article with ID {id} not found.");
            }

            var fileId = article.FileId;

            context.Articles.Remove(article);
            await context.SaveChangesAsync();

            // zdjęcie do skasowania tylko wtedy, gdy nie używa go żaden inny artykuł
            if (fileId <= 0 || await context.Articles.AnyAsync(a => a.FileId == fileId))
            {
                return null;
            }
            return fileId;
        }
    }
}

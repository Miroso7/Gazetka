using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SchoolNewspaperBlazorApp.Data;
using SchoolNewspaperBlazorApp.Interfaces.Repository;
using SchoolNewspaperBlazorApp.Interfaces.Service;

namespace SchoolNewspaperBlazorApp.Service
{
    public class ArticleService : IArticleService
    {
        private readonly IArticleRepository _articleRepository;
        public ArticleService(IArticleRepository articleRepository)
        {
            _articleRepository = articleRepository;
        }
        public async Task AddArticleAsync(string title, string text, string author, int fileId)
        {
            var article = new Article
            {
                Title = title,
                Text = text,
                Author = author,
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
            if (id <= 0)
            {
                throw new ArgumentException("Nieprawidłowe ID artykułu.", nameof(id));
            }

            // data publikacji zostaje bez zmian (repozytorium aktualizuje tylko pola poniżej)
            var article = new Article
            {
                Id = id,
                Title = title,
                Text = text,
                Author = author,
                FileId = fileId
            };
            await _articleRepository.UpdateArticleAsync(article);
        }
        public async Task RemoveArticleByIdAsync(int id)
        {
            if (id > 0)
            {
                await _articleRepository.RemoveArticleByIdAsync(id);
            }
        }
    }
}

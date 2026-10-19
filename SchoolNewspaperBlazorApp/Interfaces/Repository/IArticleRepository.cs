using SchoolNewspaperBlazorApp.Data;

namespace SchoolNewspaperBlazorApp.Interfaces.Repository
{
    public interface IArticleRepository
    {
        Task AddArticleAsync(Article article);
        Task<List<Article>> GetAllArticlesAsync();
        Task<Article> GetArticleByIdAsync(int id);
        Task UpdateArticleAsync(Article article);
        Task RemoveArticleByIdAsync(int id);
    }
}

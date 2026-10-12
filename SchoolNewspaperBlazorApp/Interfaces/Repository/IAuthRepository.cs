using SchoolNewspaperBlazorApp.Data;

namespace SchoolNewspaperBlazorApp.Interfaces.Repository
{
    public interface IAuthRepository
    {
        Task AddUserAsync(User user);
    }
}

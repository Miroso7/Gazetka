namespace SchoolNewspaperBlazorApp.Interfaces.Service
{
    public interface IAuthService
    {
        Task AddUserAsync(string login, string password, string email);
        Task<bool> LoginAsync(string email, string password);
    }
}

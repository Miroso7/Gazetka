namespace SchoolNewspaperBlazorApp.Interfaces.Service
{
    public interface IAuthService
    {
        Task AddUserAsync(string firstName, string lastName, string password, string email);
        Task<bool> LoginAsync(string email, string password);
    }
}

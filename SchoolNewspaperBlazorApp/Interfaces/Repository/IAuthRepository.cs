using SchoolNewspaperBlazorApp.Data;
using SchoolNewspaperBlazorApp.Data.Enums;

namespace SchoolNewspaperBlazorApp.Interfaces.Repository
{
    public interface IAuthRepository
    {
        Task AddUserAsync(User user);
        Task<User?> GetUserByEmailAsync(string email);
        Task<User?> GetUserByIdAsync(int userId);
        Task<bool> EmailExistsAsync(string email);
        Task UpdateUserAsync(User user);
        Task<UserProfile?> GetProfileAsync(int userId);
        Task<bool> EmailExistsForOtherAsync(string email, int exceptUserId);
        // Zmienia tylko dane profilu (nigdy hasła). Zwraca false, gdy użytkownik nie istnieje.
        Task<bool> UpdateProfileAsync(int userId, string firstName, string lastName, string email, UserRole role);
        // Zapisuje nowy hash hasła i zdejmuje wymóg zmiany hasła. Zwraca false, gdy użytkownik nie istnieje.
        Task<bool> UpdatePasswordAsync(int userId, string passwordHash);
    }
}

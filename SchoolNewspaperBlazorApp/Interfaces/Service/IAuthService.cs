using SchoolNewspaperBlazorApp.Data;
using SchoolNewspaperBlazorApp.Data.Enums;

namespace SchoolNewspaperBlazorApp.Interfaces.Service
{
    public interface IAuthService
    {
        Task AddUserAsync(string firstName, string lastName, string password, string email, UserRole role);

        // Konto zalogowanego użytkownika (bez hasła); null, gdy nikt nie jest zalogowany.
        Task<UserProfile?> GetMyProfileAsync();

        // Master edytuje dowolne konto (imię, nazwisko, e-mail, rola), pozostali tylko własne imię i nazwisko.
        // Hasła nie da się zmienić tą metodą.
        Task UpdateUserAsync(int userId, string firstName, string lastName, string email, UserRole role);
        Task<LoginResult> LoginAsync(string email, string password);

        // Zmienia hasło konta o podanym e-mailu (po sprawdzeniu obecnego hasła) i zdejmuje wymóg zmiany hasła.
        // Zwraca powód odmowy zamiast wyjątku, bo wołana jest z endpointu HTTP, a nie z komponentu.
        Task<ChangePasswordResult> ChangePasswordAsync(string email, string currentPassword, string newPassword, string confirmPassword);
    }
}

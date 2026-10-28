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

        // Zmienia zdjęcie profilowe zalogowanego użytkownika (image = null usuwa je).
        // Zdjęcie musi być JPG, PNG, GIF lub WebP i mieć do IFileService.MaxAvatarBytes.
        Task SetMyAvatarAsync(byte[]? image);

        // Master usuwa zdjęcie profilowe dowolnego użytkownika (zamiast zdjęcia pojawiają się inicjały). Pozostali dostają UnauthorizedAccessException.
        Task RemoveUserAvatarAsync(int userId);

        // Zdjęcia profilowe do podpisów artykułów: podpis autora -> adres zdjęcia.
        // Zdjęcie dostaje tylko podpis zgodny z imieniem i nazwiskiem dokładnie jednego konta (w dowolnej kolejności);
        // pozostali autorzy nie mają wpisu w słowniku, a podpis pokaże inicjały. Dostępne dla każdego, także niezalogowanych.
        Task<Dictionary<string, string>> GetAuthorAvatarUrlsAsync(IEnumerable<string> authors);
        Task<LoginResult> LoginAsync(string email, string password);

        // Zmienia hasło konta o podanym ID (po sprawdzeniu obecnego hasła) i zdejmuje wymóg zmiany hasła.
        // Zwraca powód odmowy zamiast wyjątku, bo wołana jest z endpointu HTTP, a nie z komponentu.
        Task<ChangePasswordResult> ChangePasswordAsync(int userId, string currentPassword, string newPassword, string confirmPassword);
    }
}

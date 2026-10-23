using SchoolNewspaperBlazorApp.Data;
using SchoolNewspaperBlazorApp.Data.Enums;
using System.Security.Claims;

namespace SchoolNewspaperBlazorApp.Interfaces.Repository
{
    public interface IPermissionRepository
    {
        Task<bool> HasPermissionAsync(ClaimsPrincipal principal, EnumPermissions permission);

        // Id zalogowanego użytkownika (0 = brak / niezalogowany).
        Task<int> GetUserIdAsync(ClaimsPrincipal principal);

        Task<HashSet<EnumPermissions>> GetPermissionsAsync(ClaimsPrincipal principal);

        Task<List<UserWithPermissions>> GetUsersWithPermissionsAsync();

        Task<bool> UserHasPermissionAsync(int userId, EnumPermissions permission);

        // Usuwa użytkownika miękko (IsDeleted = 1), jego uprawnienia zostają. Zwraca false, gdy użytkownik nie istnieje.
        Task<bool> DeleteUserAsync(int userId);

        // Nadaje (granted = true) albo odbiera (false) permisję. Brakujący wiersz w tabeli Permissions tworzy sam.
        Task SetPermissionAsync(int userId, EnumPermissions permission, bool granted);
    }
}

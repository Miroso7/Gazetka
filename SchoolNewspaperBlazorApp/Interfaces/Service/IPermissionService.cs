using SchoolNewspaperBlazorApp.Data;
using SchoolNewspaperBlazorApp.Data.Enums;

namespace SchoolNewspaperBlazorApp.Interfaces.Service
{
    public interface IPermissionService
    {
        Task<bool> HasPermissionAsync(EnumPermissions permission);

        // Wszystkie permisje zalogowanego użytkownika (do pokazywania / ukrywania elementów UI).
        Task<HashSet<EnumPermissions>> GetMyPermissionsAsync();

        // Id zalogowanego użytkownika (0 = niezalogowany).
        Task<int> GetMyUserIdAsync();

        // Rzuca UnauthorizedAccessException, gdy użytkownik nie ma permisji.
        Task RequirePermissionAsync(EnumPermissions permission);

        // Oba poniższe wymagają permisji ManageUser. Lista nie zawiera zalogowanego użytkownika ani kont master (chyba że zalogowany sam jest masterem).
        // Kolejność jest stała (po Id).
        Task<List<UserWithPermissions>> GetUsersWithPermissionsAsync();
        Task SetPermissionAsync(int userId, EnumPermissions permission, bool granted);

        // Usuwanie użytkownika: tylko master; nie można usunąć siebie ani innego konta master.
        Task DeleteUserAsync(int userId);
    }
}

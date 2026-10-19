using Microsoft.AspNetCore.Components.Authorization;
using SchoolNewspaperBlazorApp.Data;
using SchoolNewspaperBlazorApp.Interfaces.Repository;
using SchoolNewspaperBlazorApp.Interfaces.Service;
using System.Security.Claims;

namespace SchoolNewspaperBlazorApp.Service
{
    public class PermissionService : IPermissionService
    {
        private readonly IPermissionRepository _permissionRepository;
        private readonly AuthenticationStateProvider _asp;

        public PermissionService(IPermissionRepository permissionRepository, AuthenticationStateProvider asp)
        {
            _permissionRepository = permissionRepository;
            _asp = asp;
        }

        private async Task<ClaimsPrincipal> GetPrincipalAsync()
        {
            var authState = await _asp.GetAuthenticationStateAsync();
            return authState.User;
        }

        public async Task<bool> HasPermissionAsync(EnumPermissions permission)
        {
            return await _permissionRepository.HasPermissionAsync(await GetPrincipalAsync(), permission);
        }

        public async Task<HashSet<EnumPermissions>> GetMyPermissionsAsync()
        {
            return await _permissionRepository.GetPermissionsAsync(await GetPrincipalAsync());
        }

        public async Task RequirePermissionAsync(EnumPermissions permission)
        {
            if (!await HasPermissionAsync(permission))
            {
                throw new UnauthorizedAccessException(
                    $"Brak uprawnienia: {PermissionInfo.Label(permission)}.");
            }
        }

        public async Task<List<UserWithPermissions>> GetUsersWithPermissionsAsync()
        {
            await RequirePermissionAsync(EnumPermissions.ManageUser);
            return await _permissionRepository.GetUsersWithPermissionsAsync();
        }

        public async Task SetPermissionAsync(int userId, EnumPermissions permission, bool granted)
        {
            await RequirePermissionAsync(EnumPermissions.ManageUser);

            // zabezpieczenie przed zablokowaniem sobie dostępu do zakładki Uprawnienia
            if (!granted && permission == EnumPermissions.ManageUser)
            {
                var myId = await _permissionRepository.GetUserIdAsync(await GetPrincipalAsync());
                if (myId == userId)
                {
                    throw new InvalidOperationException(
                        "Nie możesz odebrać sobie uprawnienia „Zarządzanie uprawnieniami”.");
                }
            }

            await _permissionRepository.SetPermissionAsync(userId, permission, granted);
        }
    }
}

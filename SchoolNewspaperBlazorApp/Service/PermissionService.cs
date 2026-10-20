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

        // Uprawnienia zarezerwowane dla konta master – z panelu nie można ich nadać nikomu.
        // Master nadaje się wyłącznie ręcznie w bazie (tabela UserPermissions).
        private static bool IsMasterOnly(EnumPermissions permission) =>
            permission is EnumPermissions.Master
                       or EnumPermissions.ManageUser
                       or EnumPermissions.ManagePermissions;

        private async Task<bool> IsMasterAsync()
        {
            return await _permissionRepository.HasPermissionAsync(
                await GetPrincipalAsync(), EnumPermissions.Master);
        }

        private async Task<ClaimsPrincipal> GetPrincipalAsync()
        {
            var authState = await _asp.GetAuthenticationStateAsync();
            return authState.User;
        }

        public async Task<bool> HasPermissionAsync(EnumPermissions permission)
        {
            // zarządzanie uprawnieniami wynika wyłącznie z Master, nie z osobnego wpisu w bazie
            if (permission is EnumPermissions.ManageUser or EnumPermissions.ManagePermissions)
            {
                return await IsMasterAsync();
            }
            return await _permissionRepository.HasPermissionAsync(await GetPrincipalAsync(), permission);
        }

        public async Task<HashSet<EnumPermissions>> GetMyPermissionsAsync()
        {
            var set = await _permissionRepository.GetPermissionsAsync(await GetPrincipalAsync());

            // zarządzanie uprawnieniami ma tylko master, niezależnie od starych wpisów ManageUser w bazie
            set.Remove(EnumPermissions.ManageUser);
            set.Remove(EnumPermissions.ManagePermissions);
            if (set.Contains(EnumPermissions.Master))
            {
                set.Add(EnumPermissions.ManageUser);
            }
            return set;
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

            if (IsMasterOnly(permission))
            {
                throw new InvalidOperationException(
                    "Tego uprawnienia nie można nadawać z panelu (master jest nadawany ręcznie w bazie).");
            }

            await _permissionRepository.SetPermissionAsync(userId, permission, granted);
        }
    }
}

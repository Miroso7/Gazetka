using Microsoft.AspNetCore.Components.Authorization;
using SchoolNewspaperBlazorApp.Data;
using SchoolNewspaperBlazorApp.Interfaces.Repository;
using SchoolNewspaperBlazorApp.Interfaces.Service;

namespace SchoolNewspaperBlazorApp.Service
{
    public class PermissionService : IPermissionService
    {
        private readonly IPermissionRepository _PermissionRepository;
        private readonly AuthenticationStateProvider _asp;

        public PermissionService(IPermissionRepository PermissionRepository, AuthenticationStateProvider asp)
        {
            _PermissionRepository = PermissionRepository;
            _asp = asp;
        }

        public async Task<bool> HasPermissionAsync(EnumPermissions permission)
        {
            var authState = await _asp.GetAuthenticationStateAsync();
            var principal = authState.User;

            // repozytorium wymaga dwóch argumentów: użytkownik (z cookie) i uprawnienie
            return await _PermissionRepository.HasPermissionAsync(principal, permission);
        }
    }
}

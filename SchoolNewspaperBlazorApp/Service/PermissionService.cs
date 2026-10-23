using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using SchoolNewspaperBlazorApp.Data;
using SchoolNewspaperBlazorApp.Data.Enums;
using SchoolNewspaperBlazorApp.Interfaces.Repository;
using SchoolNewspaperBlazorApp.Interfaces.Service;
using System.Security.Claims;

namespace SchoolNewspaperBlazorApp.Service
{
    public class PermissionService : IPermissionService
    {
        private readonly IPermissionRepository _permissionRepository;
        private readonly AuthenticationStateProvider _asp;
        private readonly ISessionRegistry _sessions;
        private readonly ILogger<PermissionService> _logger;

        public PermissionService(
            IPermissionRepository permissionRepository,
            AuthenticationStateProvider asp,
            ISessionRegistry sessions,
            ILogger<PermissionService> logger)
        {
            _permissionRepository = permissionRepository;
            _asp = asp;
            _sessions = sessions;
            _logger = logger;
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
            var user = authState.User;

            // sesja wygasła albo użytkownik został wylogowany: traktujemy go jak niezalogowanego,
            // nawet jeśli obwód Blazora nadal trzyma stare uwierzytelnienie
            var sessionId = user.FindFirst(SessionRegistry.SessionClaimType)?.Value;
            if (!_sessions.IsActive(sessionId))
            {
                _logger.LogDebug("Sesja nieaktywna, użytkownik traktowany jak niezalogowany.");
                return new ClaimsPrincipal(new ClaimsIdentity());
            }

            _sessions.Touch(sessionId);
            return user;
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

        public async Task<int> GetMyUserIdAsync()
        {
            return await _permissionRepository.GetUserIdAsync(await GetPrincipalAsync());
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
                _logger.LogWarning("Odmowa dostępu: brak uprawnienia {Permission}.", permission);
                throw new UnauthorizedAccessException(
                    $"Brak uprawnienia: {PermissionInfo.Label(permission)}.");
            }
        }

        public async Task<List<UserWithPermissions>> GetUsersWithPermissionsAsync()
        {
            await RequirePermissionAsync(EnumPermissions.ManageUser);

            var users = await _permissionRepository.GetUsersWithPermissionsAsync();
            var currentId = await _permissionRepository.GetUserIdAsync(await GetPrincipalAsync());
            var viewerIsMaster = await IsMasterAsync();

            // zalogowany nie widzi siebie, a kont master nie widzi nikt, kto sam nie jest masterem
            users.RemoveAll(u => u.UserId == currentId || (u.IsMaster && !viewerIsMaster));
            return users;
        }

        public async Task DeleteUserAsync(int userId)
        {
            if (!await IsMasterAsync())
            {
                _logger.LogWarning("Odmowa usunięcia użytkownika {TargetUserId}: tylko master może usuwać użytkowników.", userId);
                throw new UnauthorizedAccessException("Tylko master może usuwać użytkowników.");
            }

            var currentId = await _permissionRepository.GetUserIdAsync(await GetPrincipalAsync());
            if (userId == currentId)
            {
                _logger.LogWarning("Użytkownik {UserId} próbował usunąć własne konto.", currentId);
                throw new InvalidOperationException("Nie możesz usunąć własnego konta.");
            }

            if (await _permissionRepository.UserHasPermissionAsync(userId, EnumPermissions.Master))
            {
                _logger.LogWarning("Użytkownik {UserId} próbował usunąć konto master {TargetUserId}.", currentId, userId);
                throw new InvalidOperationException("Nie można usunąć konta master.");
            }

            if (!await _permissionRepository.DeleteUserAsync(userId))
            {
                throw new InvalidOperationException("Nie znaleziono użytkownika.");
            }

            _logger.LogInformation("Użytkownik {UserId} usunął użytkownika {TargetUserId}.", currentId, userId);
        }

        public async Task SetPermissionAsync(int userId, EnumPermissions permission, bool granted)
        {
            await RequirePermissionAsync(EnumPermissions.ManageUser);

            var currentId = await _permissionRepository.GetUserIdAsync(await GetPrincipalAsync());
            if (IsMasterOnly(permission))
            {
                _logger.LogWarning("Użytkownik {UserId} próbował zmienić uprawnienie {Permission} użytkownikowi {TargetUserId} z panelu.",
                    currentId, permission, userId);
                throw new InvalidOperationException(
                    "Tego uprawnienia nie można nadawać z panelu (master jest nadawany ręcznie w bazie).");
            }

            await _permissionRepository.SetPermissionAsync(userId, permission, granted);
            _logger.LogInformation("Użytkownik {UserId} {Action} uprawnienie {Permission} użytkownikowi {TargetUserId}.",
                currentId, granted ? "nadał" : "odebrał", permission, userId);
        }
    }
}

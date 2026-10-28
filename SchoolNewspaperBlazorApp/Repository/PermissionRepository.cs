using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SchoolNewspaperBlazorApp.Data;
using SchoolNewspaperBlazorApp.Data.Enums;
using SchoolNewspaperBlazorApp.Interfaces.Repository;
using System.Security.Claims;

namespace SchoolNewspaperBlazorApp.Repository
{
    public class PermissionRepository : IPermissionRepository
    {
        private readonly IDbContextFactory<NewspaperDbContext> _contextFactory;
        private readonly ILogger<PermissionRepository> _logger;

        public PermissionRepository(IDbContextFactory<NewspaperDbContext> contextFactory, ILogger<PermissionRepository> logger)
        {
            _contextFactory = contextFactory;
            _logger = logger;
        }

        public async Task<int> GetUserIdAsync(ClaimsPrincipal principal)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await ResolveUserIdAsync(context, principal);
        }

        public async Task<bool> HasPermissionAsync(ClaimsPrincipal principal, EnumPermissions permission)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var userId = await ResolveUserIdAsync(context, principal);
            if (userId == 0)
            {
                _logger.LogDebug("Sprawdzanie uprawnienia {Permission} dla niezalogowanego lub nieznanego użytkownika.", permission);
                return false;
            }

            return await context.UserPermissions
                .AnyAsync(up => up.UserId == userId && up.Permission.PermissionName == permission);
        }

        public async Task<HashSet<EnumPermissions>> GetPermissionsAsync(ClaimsPrincipal principal)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var userId = await ResolveUserIdAsync(context, principal);
            if (userId == 0)
            {
                return new HashSet<EnumPermissions>();
            }

            var names = await context.UserPermissions
                .Where(up => up.UserId == userId)
                .Select(up => up.Permission.PermissionName)
                .ToListAsync();

            return names.ToHashSet();
        }

        public async Task<List<UserWithPermissions>> GetUsersWithPermissionsAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var rows = await context.Users
                .AsNoTracking()
                // stała kolejność (po Id), żeby edycja imienia, nazwiska czy roli nie przenosiła użytkownika na inne miejsce
                .OrderBy(u => u.Id)
                .Select(u => new
                {
                    u.Id,
                    u.FirstName,
                    u.LastName,
                    u.Email,
                    u.Role,
                    AvatarFileName = context.Files
                        .Where(f => f.Id == u.AvatarFileId)
                        .Select(f => f.FileName)
                        .FirstOrDefault(),
                    Names = u.UserPermission.Select(up => up.Permission.PermissionName).ToList()
                })
                .ToListAsync();

            return rows.Select(r => new UserWithPermissions
            {
                UserId = r.Id,
                FirstName = r.FirstName,
                LastName = r.LastName,
                Email = r.Email,
                Role = r.Role,
                AvatarFileName = r.AvatarFileName,
                Permissions = r.Names.ToHashSet()
            }).ToList();
        }

        public async Task<bool> UserHasPermissionAsync(int userId, EnumPermissions permission)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await context.UserPermissions
                .AnyAsync(up => up.UserId == userId && up.Permission.PermissionName == permission);
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user is null)
            {
                _logger.LogWarning("Nie można usunąć użytkownika {UserId}: nie istnieje.", userId);
                return false;
            }

            // Remove() zamienia się na soft delete (UPDATE IsDeleted = 1, patrz NewspaperDbContext);
            // uprawnienia zostają nietknięte, więc po przywróceniu konta użytkownik ma je z powrotem
            context.Users.Remove(user);
            await context.SaveChangesAsync();
            _logger.LogDebug("Oznaczono użytkownika {UserId} jako usuniętego.", userId);
            return true;
        }

        public async Task SetPermissionAsync(int userId, EnumPermissions permission, bool granted)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            if (granted)
            {
                var alreadyHas = await context.UserPermissions
                    .AnyAsync(up => up.UserId == userId && up.Permission.PermissionName == permission);
                if (alreadyHas)
                {
                    return;
                }

                // wiersz permisji tworzymy przy pierwszym nadaniu, więc tabela nie wymaga ręcznego seedowania
                var row = await context.Permissions
                    .OrderBy(p => p.Id)
                    .FirstOrDefaultAsync(p => p.PermissionName == permission);
                if (row is null)
                {
                    row = new Permission { PermissionName = permission };
                    context.Permissions.Add(row);
                    await context.SaveChangesAsync();
                    _logger.LogInformation("Utworzono brakujący wiersz uprawnienia {Permission} w tabeli Permissions.", permission);
                }

                // odebrane wcześniej uprawnienie to wiersz z IsDeleted = 1 (a indeks UserId + PermissionId jest unikalny),
                // więc zamiast dodawać duplikat przywracamy ten wiersz
                var revoked = await context.UserPermissions
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(up => up.UserId == userId && up.PermissionId == row.Id);
                if (revoked is not null)
                {
                    revoked.IsDeleted = false;
                    revoked.DeletedAt = null;
                }
                else
                {
                    context.UserPermissions.Add(new UserPermission { UserId = userId, PermissionId = row.Id });
                }
                await context.SaveChangesAsync();
                _logger.LogDebug("Nadano uprawnienie {Permission} użytkownikowi {UserId}.", permission, userId);
                return;
            }

            var links = await context.UserPermissions
                .Where(up => up.UserId == userId && up.Permission.PermissionName == permission)
                .ToListAsync();
            if (links.Count == 0)
            {
                return;
            }

            context.UserPermissions.RemoveRange(links);
            await context.SaveChangesAsync();
            _logger.LogDebug("Odebrano (soft delete) uprawnienie {Permission} użytkownikowi {UserId}.", permission, userId);
        }

        // Cookie z /auth/login zawiera niezmienne ID konta w claimie NameIdentifier (e-mail w Name jest tylko do wyświetlania,
        // bo adres może zostać zmieniony albo zwolniony i przypisany komuś innemu).
        // Zwraca 0, gdy cookie nie ma ID (np. stare cookie sprzed zmiany) albo konto już nie istnieje.
        private static async Task<int> ResolveUserIdAsync(NewspaperDbContext context, ClaimsPrincipal principal)
        {
            var claim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(claim, out var userId) || userId <= 0)
            {
                return 0;
            }

            var exists = await context.Users.AnyAsync(u => u.Id == userId);
            return exists ? userId : 0;
        }
    }
}

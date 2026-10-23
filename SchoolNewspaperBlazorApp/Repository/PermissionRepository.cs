using Microsoft.EntityFrameworkCore;
using SchoolNewspaperBlazorApp.Data;
using SchoolNewspaperBlazorApp.Data.Enums;
using SchoolNewspaperBlazorApp.Interfaces.Repository;
using System.Security.Claims;

namespace SchoolNewspaperBlazorApp.Repository
{
    public class PermissionRepository : IPermissionRepository
    {
        private readonly IDbContextFactory<NewspaperDbContext> _contextFactory;

        public PermissionRepository(IDbContextFactory<NewspaperDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
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
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .ThenBy(u => u.Email)
                .Select(u => new
                {
                    u.Id,
                    u.FirstName,
                    u.LastName,
                    u.Email,
                    Names = u.UserPermission.Select(up => up.Permission.PermissionName).ToList()
                })
                .ToListAsync();

            return rows.Select(r => new UserWithPermissions
            {
                UserId = r.Id,
                FirstName = r.FirstName,
                LastName = r.LastName,
                Email = r.Email,
                Permissions = r.Names.ToHashSet()
            }).ToList();
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
                }

                context.UserPermissions.Add(new UserPermission { UserId = userId, PermissionId = row.Id });
                await context.SaveChangesAsync();
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
        }

        // Cookie z /auth/login zawiera adres e-mail w claimie Name.
        private static async Task<int> ResolveUserIdAsync(NewspaperDbContext context, ClaimsPrincipal principal)
        {
            var email = principal.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrWhiteSpace(email))
            {
                return 0;
            }

            return await context.Users
                .Where(u => u.Email == email)
                .Select(u => u.Id)
                .FirstOrDefaultAsync();
        }
    }
}

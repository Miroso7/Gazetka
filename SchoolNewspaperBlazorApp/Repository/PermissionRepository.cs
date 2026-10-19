using Microsoft.EntityFrameworkCore;
using SchoolNewspaperBlazorApp.Data;
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

        public async Task<bool> HasPermissionAsync(ClaimsPrincipal principal, EnumPermissions permission)
        {
            // Cookie z /auth/login zawiera adres e-mail w claimie Name.
            var email = principal.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            await using var context = await _contextFactory.CreateDbContextAsync();

            var userId = await context.Users
                .Where(u => u.Email == email)
                .Select(u => u.Id)
                .FirstOrDefaultAsync();

            if (userId == 0)
            {
                return false;
            }

            return await context.UserPermissions
                .AnyAsync(up => up.UserId == userId && up.Permission.PermissionName == permission);
        }
    }
}

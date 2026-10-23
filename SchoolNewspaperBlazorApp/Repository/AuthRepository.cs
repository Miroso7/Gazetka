using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SchoolNewspaperBlazorApp.Data;
using SchoolNewspaperBlazorApp.Data.Enums;
using SchoolNewspaperBlazorApp.Interfaces.Repository;

namespace SchoolNewspaperBlazorApp.Repository
{
    public class AuthRepository : IAuthRepository
    {
        private readonly IDbContextFactory<NewspaperDbContext> _contextFactory;
        private readonly ILogger<AuthRepository> _logger;

        public AuthRepository(IDbContextFactory<NewspaperDbContext> contextFactory, ILogger<AuthRepository> logger)
        {
            _contextFactory = contextFactory;
            _logger = logger;
        }

        public async Task AddUserAsync(User user)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();
            _logger.LogDebug("Zapisano użytkownika {UserId} w bazie.", user.Id);
        }

        // Zwraca null, gdy nie ma użytkownika o takim adresie e-mail.
        public async Task<User?> GetUserByEmailAsync(string email)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await context.Users.AnyAsync(u => u.Email == email);
        }

        public async Task UpdateUserAsync(User user)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            context.Users.Update(user);
            await context.SaveChangesAsync();
            _logger.LogDebug("Zaktualizowano użytkownika {UserId} w bazie.", user.Id);
        }

        public async Task<UserProfile?> GetProfileAsync(int userId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await context.Users
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => new UserProfile
                {
                    Id = u.Id,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Email = u.Email,
                    Role = u.Role
                })
                .FirstOrDefaultAsync();
        }

        public async Task<bool> EmailExistsForOtherAsync(string email, int exceptUserId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await context.Users.AnyAsync(u => u.Email == email && u.Id != exceptUserId);
        }

        public async Task<bool> UpdateProfileAsync(int userId, string firstName, string lastName, string email, UserRole role)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user is null)
            {
                _logger.LogWarning("Nie można zaktualizować profilu: użytkownik {UserId} nie istnieje.", userId);
                return false;
            }

            // celowo tylko te pola – hasło zostaje nietknięte
            user.FirstName = firstName;
            user.LastName = lastName;
            user.Email = email;
            user.Role = role;

            await context.SaveChangesAsync();
            _logger.LogDebug("Zaktualizowano profil użytkownika {UserId}.", userId);
            return true;
        }
    }
}

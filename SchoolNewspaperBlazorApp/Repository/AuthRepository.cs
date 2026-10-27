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

        // Zwraca null, gdy nie ma użytkownika o takim ID (albo został usunięty).
        public async Task<User?> GetUserByIdAsync(int userId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);
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
                    Role = u.Role,
                    AvatarFileName = context.Files
                        .Where(f => f.Id == u.AvatarFileId)
                        .Select(f => f.FileName)
                        .FirstOrDefault()
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

        public async Task<bool> UpdatePasswordAsync(int userId, string passwordHash)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user is null)
            {
                _logger.LogWarning("Nie można zmienić hasła: użytkownik {UserId} nie istnieje.", userId);
                return false;
            }

            // celowo tylko te pola – reszta konta zostaje nietknięta
            user.Password = passwordHash;
            user.MustChangePassword = false;

            await context.SaveChangesAsync();
            _logger.LogDebug("Zapisano nowe hasło użytkownika {UserId}.", userId);
            return true;
        }

        public async Task<bool> SetAvatarAsync(int userId, int? fileId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user is null)
            {
                _logger.LogWarning("Nie można zmienić zdjęcia profilowego: użytkownik {UserId} nie istnieje.", userId);
                return false;
            }

            var previousFileId = user.AvatarFileId;
            user.AvatarFileId = fileId;

            // poprzednie zdjęcie znika razem ze zmianą (soft delete, plik zostaje na dysku), wszystko w jednym zapisie
            if (previousFileId is > 0 && previousFileId != fileId)
            {
                var previous = await context.Files.FirstOrDefaultAsync(f => f.Id == previousFileId);
                if (previous is not null)
                {
                    context.Files.Remove(previous);
                }
            }

            await context.SaveChangesAsync();
            _logger.LogDebug("Zapisano zdjęcie profilowe użytkownika {UserId} (FileId {FileId}).", userId, fileId);
            return true;
        }

        public async Task<List<UserNameAvatar>> GetUserNamesWithAvatarsAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await context.Users
                .AsNoTracking()
                .Select(u => new UserNameAvatar(
                    u.FirstName,
                    u.LastName,
                    context.Files
                        .Where(f => f.Id == u.AvatarFileId)
                        .Select(f => f.FileName)
                        .FirstOrDefault()))
                .ToListAsync();
        }
    }
}

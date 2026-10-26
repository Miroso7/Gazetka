using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SchoolNewspaperBlazorApp.Data;
using SchoolNewspaperBlazorApp.Data.Enums;
using SchoolNewspaperBlazorApp.Interfaces.Repository;

namespace SchoolNewspaperBlazorApp.Repository
{
    public class SchoolDataRepository : ISchoolDataRepository
    {
        private readonly IDbContextFactory<NewspaperDbContext> _contextFactory;
        private readonly ILogger<SchoolDataRepository> _logger;

        public SchoolDataRepository(IDbContextFactory<NewspaperDbContext> contextFactory, ILogger<SchoolDataRepository> logger)
        {
            _contextFactory = contextFactory;
            _logger = logger;
        }

        public async Task<List<SchoolDictionaryItem>> GetItemsAsync(SchoolDictionaryKind kind)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await (kind switch
            {
                SchoolDictionaryKind.Class => ListAsync(context.SchoolClasses),
                SchoolDictionaryKind.Teacher => ListAsync(context.Teachers),
                SchoolDictionaryKind.Room => ListAsync(context.Rooms),
                SchoolDictionaryKind.Subject => ListAsync(context.Subjects),
                _ => throw new ArgumentOutOfRangeException(nameof(kind))
            });
        }

        public async Task<bool> ExistsAsync(SchoolDictionaryKind kind, int id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await (kind switch
            {
                SchoolDictionaryKind.Class => context.SchoolClasses.AnyAsync(x => x.Id == id),
                SchoolDictionaryKind.Teacher => context.Teachers.AnyAsync(x => x.Id == id),
                SchoolDictionaryKind.Room => context.Rooms.AnyAsync(x => x.Id == id),
                SchoolDictionaryKind.Subject => context.Subjects.AnyAsync(x => x.Id == id),
                _ => throw new ArgumentOutOfRangeException(nameof(kind))
            });
        }

        public async Task<bool> NameExistsAsync(SchoolDictionaryKind kind, string name, int? excludeId = null)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            // porównanie w bazie nie rozróżnia wielkości liter (domyślne sortowanie SQL Server)
            return await (kind switch
            {
                SchoolDictionaryKind.Class => context.SchoolClasses.AnyAsync(x => x.Name == name && (excludeId == null || x.Id != excludeId)),
                SchoolDictionaryKind.Teacher => context.Teachers.AnyAsync(x => x.Name == name && (excludeId == null || x.Id != excludeId)),
                SchoolDictionaryKind.Room => context.Rooms.AnyAsync(x => x.Name == name && (excludeId == null || x.Id != excludeId)),
                SchoolDictionaryKind.Subject => context.Subjects.AnyAsync(x => x.Name == name && (excludeId == null || x.Id != excludeId)),
                _ => throw new ArgumentOutOfRangeException(nameof(kind))
            });
        }

        public async Task AddItemAsync(SchoolDictionaryKind kind, string name)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            switch (kind)
            {
                case SchoolDictionaryKind.Class: context.SchoolClasses.Add(new SchoolClass { Name = name }); break;
                case SchoolDictionaryKind.Teacher: context.Teachers.Add(new Teacher { Name = name }); break;
                case SchoolDictionaryKind.Room: context.Rooms.Add(new Room { Name = name }); break;
                case SchoolDictionaryKind.Subject: context.Subjects.Add(new Subject { Name = name }); break;
                default: throw new ArgumentOutOfRangeException(nameof(kind));
            }

            await context.SaveChangesAsync();
            _logger.LogDebug("Zapisano pozycję słownika {Kind} w bazie.", kind);
        }

        public async Task<bool> UpdateItemAsync(SchoolDictionaryKind kind, int id, string name)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            SchoolDictionaryItem? item = kind switch
            {
                SchoolDictionaryKind.Class => await context.SchoolClasses.FirstOrDefaultAsync(x => x.Id == id),
                SchoolDictionaryKind.Teacher => await context.Teachers.FirstOrDefaultAsync(x => x.Id == id),
                SchoolDictionaryKind.Room => await context.Rooms.FirstOrDefaultAsync(x => x.Id == id),
                SchoolDictionaryKind.Subject => await context.Subjects.FirstOrDefaultAsync(x => x.Id == id),
                _ => throw new ArgumentOutOfRangeException(nameof(kind))
            };
            if (item is null)
            {
                _logger.LogWarning("Nie można zaktualizować pozycji {Kind} {ItemId}: nie istnieje.", kind, id);
                return false;
            }

            item.Name = name;
            await context.SaveChangesAsync();
            _logger.LogDebug("Zaktualizowano pozycję {Kind} {ItemId}.", kind, id);
            return true;
        }

        public async Task<bool> RemoveItemAsync(SchoolDictionaryKind kind, int id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            SchoolDictionaryItem? item = kind switch
            {
                SchoolDictionaryKind.Class => await context.SchoolClasses.FirstOrDefaultAsync(x => x.Id == id),
                SchoolDictionaryKind.Teacher => await context.Teachers.FirstOrDefaultAsync(x => x.Id == id),
                SchoolDictionaryKind.Room => await context.Rooms.FirstOrDefaultAsync(x => x.Id == id),
                SchoolDictionaryKind.Subject => await context.Subjects.FirstOrDefaultAsync(x => x.Id == id),
                _ => throw new ArgumentOutOfRangeException(nameof(kind))
            };
            if (item is null)
            {
                _logger.LogWarning("Nie można usunąć pozycji {Kind} {ItemId}: nie istnieje.", kind, id);
                return false;
            }

            // Remove() zamienia się na soft delete (UPDATE IsDeleted = 1, patrz NewspaperDbContext)
            context.Remove(item);
            await context.SaveChangesAsync();
            _logger.LogDebug("Oznaczono pozycję {Kind} {ItemId} jako usuniętą.", kind, id);
            return true;
        }

        public async Task<int> CountUsageAsync(SchoolDictionaryKind kind, int id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await (kind switch
            {
                SchoolDictionaryKind.Class => context.Substitutions.CountAsync(s => s.ClassId == id),
                SchoolDictionaryKind.Teacher => context.Substitutions.CountAsync(s => s.AbsentTeacherId == id || s.SubstituteTeacherId == id),
                SchoolDictionaryKind.Room => context.Substitutions.CountAsync(s => s.RoomId == id),
                SchoolDictionaryKind.Subject => context.Substitutions.CountAsync(s => s.SubjectId == id),
                _ => throw new ArgumentOutOfRangeException(nameof(kind))
            });
        }

        private static async Task<List<SchoolDictionaryItem>> ListAsync<T>(IQueryable<T> set) where T : SchoolDictionaryItem
        {
            var items = await set.AsNoTracking().OrderBy(x => x.Name).ToListAsync();
            return items.Cast<SchoolDictionaryItem>().ToList();
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SchoolNewspaperBlazorApp.Data;
using SchoolNewspaperBlazorApp.Interfaces.Repository;

namespace SchoolNewspaperBlazorApp.Repository
{
    public class FileRepository : IFileRepository
    {
        private readonly IDbContextFactory<NewspaperDbContext> _contextFactory;
        private readonly ILogger<FileRepository> _logger;

        public FileRepository(IDbContextFactory<NewspaperDbContext> contextFactory, ILogger<FileRepository> logger)
        {
            _contextFactory = contextFactory;
            _logger = logger;
        }

        public async Task<int> GetLastFileId()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var id = await context.Files
                .OrderByDescending(x => x.Id)
                .Select(x => x.Id)
                .FirstOrDefaultAsync();
            return id + 1;
        }

        public async Task<MediaFile> GetFileByIdAsync(int id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var fileId = await context.Files.FirstOrDefaultAsync(a => a.Id == id);
            if (fileId == null)
            {
                _logger.LogWarning("Nie znaleziono zdjęcia {FileId} w bazie danych.", id);
                throw new Exception("[FileRepository]Brak zdjecia w bazie danych");
            }
            return fileId;
        }

        public async Task<Dictionary<int, string>> GetFileNamesByIdsAsync(IEnumerable<int> ids)
        {
            var idList = ids.Distinct().ToList();

            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Files
                .AsNoTracking()
                .Where(f => idList.Contains(f.Id))
                .ToDictionaryAsync(f => f.Id, f => f.FileName);
        }

        public async Task<string?> DeleteFileAsync(int id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var file = await context.Files.FirstOrDefaultAsync(f => f.Id == id);
            if (file == null)
            {
                _logger.LogDebug("Zdjęcie {FileId} już nie istnieje w bazie.", id);
                return null;
            }

            var fileName = file.FileName;
            context.Files.Remove(file);
            await context.SaveChangesAsync();
            _logger.LogDebug("Usunięto zdjęcie {FileId} z bazy.", id);
            return fileName;
        }

        public async Task AddFileAsync(Data.MediaFile file)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            await context.Files.AddAsync(file);
            await context.SaveChangesAsync();
            _logger.LogDebug("Zapisano zdjęcie {FileId} w bazie.", file.Id);
        }
    }
}

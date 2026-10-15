using Microsoft.EntityFrameworkCore;
using SchoolNewspaperBlazorApp.Data;
using SchoolNewspaperBlazorApp.Interfaces.Repository;

namespace SchoolNewspaperBlazorApp.Repository
{
    public class FileRepository : IFileRepository
    {
        private readonly IDbContextFactory<NewspaperDbContext> _contextFactory;

        public FileRepository(IDbContextFactory<NewspaperDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
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
                throw new Exception("[FileRepository]Brak zdjecia w bazie danych");
            }
            return fileId;
        }

        public async Task AddFileAsync(Data.MediaFile file)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            await context.Files.AddAsync(file);
            await context.SaveChangesAsync();
        }
    }
}

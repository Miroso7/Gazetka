using SchoolNewspaperBlazorApp.Data;

namespace SchoolNewspaperBlazorApp.Interfaces.Repository
{
    public interface IFileRepository
    {
        Task<int> GetLastFileId();
        Task AddFileAsync(MediaFile file);
        Task<MediaFile> GetFileByIdAsync(int id);
        // Oznacza wpis o zdjęciu jako usunięty (soft delete) i zwraca nazwę pliku (null, gdy wpisu nie było).
        Task<string?> DeleteFileAsync(int id);
        Task<Dictionary<int, string>> GetFileNamesByIdsAsync(IEnumerable<int> ids);

    }
}

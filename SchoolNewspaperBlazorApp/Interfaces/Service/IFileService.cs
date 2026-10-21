using Microsoft.AspNetCore.Components.Forms;
using SchoolNewspaperBlazorApp.Data;

namespace SchoolNewspaperBlazorApp.Interfaces.Service
{
    public interface IFileService
    {
        Task<string> GetPreviewAsync(IBrowserFile file);
        Task<int> UploadImage(IBrowserFile file);
        Task<string> GetFileNameByIdAsync(int id);
        Task<Dictionary<int, string>> GetFileNamesByIdsAsync(IEnumerable<int> ids);
        // Usuwa zdjęcie: wpis w bazie i plik z folderu Images.
        Task DeleteFileAsync(int id);

    }
}

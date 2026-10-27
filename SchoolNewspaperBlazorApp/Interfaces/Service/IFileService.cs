using Microsoft.AspNetCore.Components.Forms;
using SchoolNewspaperBlazorApp.Data;

namespace SchoolNewspaperBlazorApp.Interfaces.Service
{
    public interface IFileService
    {
        Task<string> GetPreviewAsync(IBrowserFile file);
        Task<int> UploadImage(IBrowserFile file);
        // Wgrywa kilka zdjęć naraz (np. do galerii) i zwraca ich Id w tej samej kolejności.
        // Gdy któreś jest nieprawidłowe, nie zapisuje żadnego.
        Task<List<int>> UploadImages(IReadOnlyList<byte[]> images);
        // Maksymalny rozmiar zdjęcia profilowego (po zmniejszeniu w przeglądarce zwykle zajmuje kilkaset KB).
        const int MaxAvatarBytes = 2 * 1024 * 1024;
        // Wgrywa zdjęcie profilowe i zwraca Id pliku. Może to zrobić każdy zalogowany użytkownik (bez uprawnień do artykułów).
        Task<int> UploadAvatarAsync(byte[] image);
        // Czy zawartość pliku to obsługiwane zdjęcie (JPG, PNG, GIF, WebP).
        bool IsSupportedImage(byte[] bytes);
        // Usuwa zdjęcia (wpis w bazie i plik) przy edycji artykułu; wymaga edycji albo usuwania artykułów.
        Task DeleteFilesAsync(IEnumerable<int> ids);
        Task<string> GetFileNameByIdAsync(int id);
        Task<Dictionary<int, string>> GetFileNamesByIdsAsync(IEnumerable<int> ids);
        // Usuwa zdjęcie: wpis w bazie i plik z folderu Images.
        Task DeleteFileAsync(int id);

    }
}

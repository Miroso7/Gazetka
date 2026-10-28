using Microsoft.AspNetCore.Components.Forms;
using SchoolNewspaperBlazorApp.Data;

namespace SchoolNewspaperBlazorApp.Interfaces.Service
{
    public interface IFileService
    {
        // Limity zdjęć wybieranych w przeglądarce: oryginał do 25 MB, po zmniejszeniu do 1920 px najwyżej 5 MB (GIF-y nie są zmniejszane, więc od razu do 5 MB).
        const long MaxOriginalImageBytes = 25 * 1024 * 1024;
        const long MaxStoredImageBytes = 5 * 1024 * 1024;
        const int MaxImageDimension = 1920;
        // Podgląd wybranego zdjęcia (adres data:) – zmniejszony w przeglądarce, żeby nie przesyłać całego pliku.
        Task<string> GetPreviewAsync(IBrowserFile file);
        // Zdjęcie do zapisu: zmniejszone w przeglądarce do MaxImageDimension px (GIF bez zmian), zanim trafi na serwer.
        // Rzuca InvalidOperationException, gdy plik przekracza limity.
        Task<byte[]> ReadImageAsync(IBrowserFile file);
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

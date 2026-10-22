using Microsoft.AspNetCore.Components.Forms;
using SchoolNewspaperBlazorApp.Data;
using SchoolNewspaperBlazorApp.Data.Enums;
using SchoolNewspaperBlazorApp.Interfaces.Repository;
using SchoolNewspaperBlazorApp.Interfaces.Service;

namespace SchoolNewspaperBlazorApp.Service
{
    public class FileService : IFileService
    {
        private readonly IFileRepository _fileRepository;
        private readonly IPermissionService _permissionService;
        public FileService(IFileRepository fileRepository, IPermissionService permissionService)
        {
            _fileRepository = fileRepository;
            _permissionService = permissionService;
        }
        private readonly string uploadPath = @"C:\Users\Uczeń2026\source\repos\Gazetka\Images";
        public async Task<string> GetPreviewAsync(IBrowserFile file)
        {
            using var stream = file.OpenReadStream(5 * 1024 * 1024);
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            byte[] bytes = memoryStream.ToArray();
            return $"data:{file.ContentType};base64,{Convert.ToBase64String(bytes)}";
        }
        public async Task<int> UploadImage(IBrowserFile file)
        {
            // zdjęcia wgrywa się tylko przy dodawaniu albo edycji artykułu
            if (!await _permissionService.HasPermissionAsync(EnumPermissions.AddArticle)
                && !await _permissionService.HasPermissionAsync(EnumPermissions.EditArticle))
            {
                throw new UnauthorizedAccessException("Brak uprawnienia do dodawania zdjęć.");
            }
            // wczytujemy plik do pamięci i rozpoznajemy go po zawartości, a nie po nazwie ani typie od przeglądarki
            using var stream = file.OpenReadStream(5 * 1024 * 1024);
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            byte[] bytes = memoryStream.ToArray();

            // rozszerzenie wybiera serwer (tylko zdjęcia), więc nie da się wgrać np. HTML czy SVG ze skryptem
            string? extension = DetectImageExtension(bytes);
            if (extension is null)
            {
                throw new InvalidOperationException("Dozwolone są tylko zdjęcia w formacie JPG, PNG, GIF lub WebP.");
            }

            //Tworzenie folderu
            Directory.CreateDirectory(uploadPath);
            //Generowanie nazwy + extension np. plik.png
            string fileName = $"{Guid.NewGuid()}{extension}";
            // łaczenie uploadPath i fileName C:\Users\Praktyka\source\repos\SchoolNewspaperBlazorApp\Images\plik.png
            string fullPath = Path.Combine(uploadPath, fileName);

            await File.WriteAllBytesAsync(fullPath, bytes);
            var id = await _fileRepository.GetLastFileId();
            var mediaFile = new MediaFile
            {
                Id = id,
                FileName = fileName,
                FileType = extension
            };
            await _fileRepository.AddFileAsync(mediaFile);

            return id;

        }
        // Rozpoznaje format po początku pliku (JPG, PNG, GIF, WebP); dla innych plików zwraca null.
        private static string? DetectImageExtension(byte[] bytes)
        {
            ReadOnlySpan<byte> data = bytes;

            if (data.StartsWith(new byte[] { 0xFF, 0xD8, 0xFF }))
            {
                return ".jpg";
            }
            if (data.StartsWith(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
            {
                return ".png";
            }
            if (data.StartsWith("GIF87a"u8) || data.StartsWith("GIF89a"u8))
            {
                return ".gif";
            }
            if (data.Length >= 12 && data[..4].SequenceEqual("RIFF"u8) && data.Slice(8, 4).SequenceEqual("WEBP"u8))
            {
                return ".webp";
            }
            return null;
        }

        public Task<Dictionary<int, string>> GetFileNamesByIdsAsync(IEnumerable<int> ids)
            => _fileRepository.GetFileNamesByIdsAsync(ids);

        public async Task DeleteFileAsync(int id)
        {
            await _permissionService.RequirePermissionAsync(EnumPermissions.DeleteArticle);
            if (id <= 0) return;

            var fileName = await _fileRepository.DeleteFileAsync(id);
            if (string.IsNullOrWhiteSpace(fileName)) return;

            // GetFileName odcina ewentualne ścieżki, więc nie da się wyjść poza folder Images
            var fullPath = Path.Combine(uploadPath, Path.GetFileName(fileName));
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }

        public async Task<string> GetFileNameByIdAsync(int id)
        {
            var file = await _fileRepository.GetFileByIdAsync(id);
            return file.FileName;
        }
    }
}

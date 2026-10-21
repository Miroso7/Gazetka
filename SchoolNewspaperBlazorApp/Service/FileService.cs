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
            //Tworzenie folderu
            Directory.CreateDirectory(uploadPath);
            //Pobieranie extension z pliku czyli np .png
            string extension = Path.GetExtension(file.Name);
            //Generowanie nazwy + extension np. plik.png
            string fileName = $"{Guid.NewGuid()}{extension}";
            // łaczenie uploadPath i fileName C:\Users\Praktyka\source\repos\SchoolNewspaperBlazorApp\Images\plik.png
            string fullPath = Path.Combine(uploadPath, fileName);

            using var stream = file.OpenReadStream(5 * 1024 * 1024);
            using var fileStream = new FileStream(
                fullPath, //pelny link odnosnika
                FileMode.Create //ustawienie tworzenia
                );
            await stream.CopyToAsync(fileStream);
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

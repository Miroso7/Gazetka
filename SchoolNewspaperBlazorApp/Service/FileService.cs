using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
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
        private readonly ILogger<FileService> _logger;
        public FileService(IFileRepository fileRepository, IPermissionService permissionService, ILogger<FileService> logger)
        {
            _fileRepository = fileRepository;
            _permissionService = permissionService;
            _logger = logger;
        }
        private readonly string uploadPath = @"C:\Users\Uczeń2026\source\repos\Gazetka\Images";
        // podgląd jest mniejszy niż zdjęcie do zapisu (wystarczy na okienko formularza), więc nie obciąża połączenia
        private const int PreviewDimension = 960;

        public async Task<string> GetPreviewAsync(IBrowserFile file)
        {
            EnsureWithinOriginalLimit(file);

            // GIF zostaje bez zmian (animacja), reszta jest zmniejszana w przeglądarce
            var isGif = IsGif(file);
            var source = isGif ? file : await RequestResizedAsync(file, PreviewDimension);
            var contentType = isGif ? "image/gif" : ResizedFormat(file);

            byte[] bytes = await ReadAllAsync(source, IFileService.MaxStoredImageBytes, isGif);
            return $"data:{contentType};base64,{Convert.ToBase64String(bytes)}";
        }

        public async Task<byte[]> ReadImageAsync(IBrowserFile file)
        {
            EnsureWithinOriginalLimit(file);

            // przeglądarka zmniejsza zdjęcie do 1920 px i dopiero zmniejszone idzie na serwer; GIF zostaje bez zmian
            var isGif = IsGif(file);
            var source = isGif ? file : await RequestResizedAsync(file, IFileService.MaxImageDimension);
            return await ReadAllAsync(source, IFileService.MaxStoredImageBytes, isGif);
        }

        private static void EnsureWithinOriginalLimit(IBrowserFile file)
        {
            if (file.Size > IFileService.MaxOriginalImageBytes)
            {
                throw new InvalidOperationException("Zdjęcie może mieć maksymalnie 25 MB.");
            }
        }

        private static bool IsGif(IBrowserFile file) =>
            string.Equals(file.ContentType, "image/gif", StringComparison.OrdinalIgnoreCase);

        // format po zmniejszeniu jest ten sam co oryginału (PNG zachowuje przezroczystość), nieznane typy zamieniamy na JPEG
        private static string ResizedFormat(IBrowserFile file) => file.ContentType?.ToLowerInvariant() switch
        {
            "image/png" => "image/png",
            "image/webp" => "image/webp",
            _ => "image/jpeg"
        };

        private async Task<IBrowserFile> RequestResizedAsync(IBrowserFile file, int maxDimension)
        {
            try
            {
                return await file.RequestImageFileAsync(ResizedFormat(file), maxDimension, maxDimension);
            }
            catch (Exception ex) when (ex is JSException or IOException)
            {
                _logger.LogWarning(ex, "Przeglądarka nie zmniejszyła zdjęcia {FileName}.", file.Name);
                throw new InvalidOperationException("Nie udało się odczytać zdjęcia. Wybierz plik JPG, PNG, GIF lub WebP.");
            }
        }

        private async Task<byte[]> ReadAllAsync(IBrowserFile file, long maxBytes, bool original)
        {
            try
            {
                using var stream = file.OpenReadStream(maxBytes);
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                return memoryStream.ToArray();
            }
            catch (IOException ex)
            {
                // OpenReadStream rzuca IOException, gdy plik przekracza dozwolony rozmiar
                _logger.LogWarning(ex, "Zdjęcie {FileName} przekracza {MaxMb} MB.", file.Name, maxBytes / (1024 * 1024));
                throw new InvalidOperationException(original
                    ? "Animowany GIF może mieć maksymalnie 5 MB."
                    : "Zdjęcie po zmniejszeniu nadal przekracza 5 MB. Wybierz mniejsze.");
            }
        }
        private async Task EnsureCanUploadAsync()
        {
            // zdjęcia wgrywa się tylko przy dodawaniu albo edycji artykułu
            if (!await _permissionService.HasPermissionAsync(EnumPermissions.AddArticle)
                && !await _permissionService.HasPermissionAsync(EnumPermissions.EditArticle))
            {
                _logger.LogWarning("Odmowa wgrania zdjęcia: brak uprawnienia do dodawania lub edycji artykułów.");
                throw new UnauthorizedAccessException("Brak uprawnienia do dodawania zdjęć.");
            }
        }
        public async Task<int> UploadImage(IBrowserFile file)
        {
            await EnsureCanUploadAsync();
            // plik jest wcześniej zmniejszany w przeglądarce, a rozpoznajemy go po zawartości, nie po nazwie ani typie od przeglądarki
            byte[] bytes = await ReadImageAsync(file);

            return await StoreImageAsync(bytes);
        }
        public async Task<List<int>> UploadImages(IReadOnlyList<byte[]> images)
        {
            await EnsureCanUploadAsync();

            // najpierw sprawdzamy wszystkie zdjęcia, żeby przy błędzie nic nie trafiło na dysk
            if (images.Any(image => !IsSupportedImage(image)))
            {
                _logger.LogWarning("Odrzucono wgrywanie {Count} zdjęć: co najmniej jeden plik nie jest obsługiwanym zdjęciem.", images.Count);
                throw new InvalidOperationException("Dozwolone są tylko zdjęcia w formacie JPG, PNG, GIF lub WebP.");
            }

            var ids = new List<int>();
            try
            {
                foreach (var image in images)
                {
                    ids.Add(await StoreImageAsync(image));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Nie udało się wgrać zdjęć (zapisano {Stored} z {Count}), wycofuję zapisane.", ids.Count, images.Count);
                // nie zostawiamy po sobie części wgranych zdjęć
                foreach (var id in ids)
                {
                    try { await RemoveStoredFileAsync(id); }
                    catch (Exception rollbackEx)
                    {
                        _logger.LogWarning(rollbackEx, "Nie udało się wycofać zdjęcia (FileId {FileId}) po błędzie wgrywania.", id);
                    }
                }
                throw;
            }
            return ids;
        }
        public async Task<int> UploadAvatarAsync(byte[] image)
        {
            if (await _permissionService.GetMyUserIdAsync() == 0)
            {
                _logger.LogWarning("Odmowa wgrania zdjęcia profilowego: użytkownik nie jest zalogowany.");
                throw new UnauthorizedAccessException("Zaloguj się, aby zmienić zdjęcie profilowe.");
            }

            if (image.Length > IFileService.MaxAvatarBytes)
            {
                throw new InvalidOperationException("Zdjęcie profilowe może mieć maksymalnie 2 MB.");
            }

            return await StoreImageAsync(image);
        }
        public bool IsSupportedImage(byte[] bytes) => DetectImageExtension(bytes) is not null;

        private async Task<int> StoreImageAsync(byte[] bytes)
        {
            // rozszerzenie wybiera serwer (tylko zdjęcia), więc nie da się wgrać np. HTML czy SVG ze skryptem
            string? extension = DetectImageExtension(bytes);
            if (extension is null)
            {
                _logger.LogWarning("Odrzucono plik: zawartość nie jest obsługiwanym zdjęciem.");
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
            _logger.LogInformation("Zapisano zdjęcie {FileName} (FileId {FileId}, {Size} B).", fileName, id, bytes.Length);

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
            await RemoveStoredFileAsync(id);
        }

        public async Task DeleteFilesAsync(IEnumerable<int> ids)
        {
            if (!await _permissionService.HasPermissionAsync(EnumPermissions.EditArticle)
                && !await _permissionService.HasPermissionAsync(EnumPermissions.DeleteArticle))
            {
                _logger.LogWarning("Odmowa usunięcia zdjęć: brak uprawnienia do edycji lub usuwania artykułów.");
                throw new UnauthorizedAccessException("Brak uprawnienia do usuwania zdjęć.");
            }

            foreach (var id in ids.Distinct())
            {
                await RemoveStoredFileAsync(id);
            }
        }

        private async Task RemoveStoredFileAsync(int id)
        {
            if (id <= 0) return;

            // soft delete: wpis dostaje IsDeleted = 1, a sam plik zostaje na dysku (da się go przywrócić)
            var fileName = await _fileRepository.DeleteFileAsync(id);
            if (string.IsNullOrWhiteSpace(fileName)) return;

            _logger.LogInformation("Oznaczono zdjęcie {FileName} (FileId {FileId}) jako usunięte, plik zostaje na dysku.", fileName, id);
        }

        public async Task<string> GetFileNameByIdAsync(int id)
        {
            var file = await _fileRepository.GetFileByIdAsync(id);
            return file.FileName;
        }
    }
}

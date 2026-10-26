using Microsoft.Extensions.Logging;
using SchoolNewspaperBlazorApp.Data;
using SchoolNewspaperBlazorApp.Data.Enums;
using SchoolNewspaperBlazorApp.Interfaces.Repository;
using SchoolNewspaperBlazorApp.Interfaces.Service;

namespace SchoolNewspaperBlazorApp.Service
{
    public class SchoolDataService : ISchoolDataService
    {
        private readonly ISchoolDataRepository _repository;
        private readonly IPermissionService _permissionService;
        private readonly ILogger<SchoolDataService> _logger;

        public SchoolDataService(
            ISchoolDataRepository repository,
            IPermissionService permissionService,
            ILogger<SchoolDataService> logger)
        {
            _repository = repository;
            _permissionService = permissionService;
            _logger = logger;
        }

        public async Task<List<SchoolDictionaryItem>> GetItemsAsync(SchoolDictionaryKind kind)
        {
            ValidateKind(kind);
            return await _repository.GetItemsAsync(kind);
        }

        public async Task AddItemAsync(SchoolDictionaryKind kind, string name)
        {
            await _permissionService.RequirePermissionAsync(EnumPermissions.ManageSchoolData);
            ValidateKind(kind);
            var clean = CleanName(kind, name);

            if (await _repository.NameExistsAsync(kind, clean))
            {
                throw new InvalidOperationException($"Pozycja „{clean}” już istnieje.");
            }

            await _repository.AddItemAsync(kind, clean);
            _logger.LogInformation("Dodano pozycję słownika {Kind}.", kind);
        }

        public async Task UpdateItemAsync(SchoolDictionaryKind kind, int id, string name)
        {
            await _permissionService.RequirePermissionAsync(EnumPermissions.ManageSchoolData);
            ValidateKind(kind);
            if (id <= 0)
            {
                throw new ArgumentException("Nieprawidłowe ID pozycji.", nameof(id));
            }
            var clean = CleanName(kind, name);

            if (await _repository.NameExistsAsync(kind, clean, id))
            {
                throw new InvalidOperationException($"Pozycja „{clean}” już istnieje.");
            }

            if (!await _repository.UpdateItemAsync(kind, id, clean))
            {
                throw new InvalidOperationException("Nie znaleziono pozycji. Odśwież listę.");
            }
            _logger.LogInformation("Zaktualizowano pozycję słownika {Kind} {ItemId}.", kind, id);
        }

        public async Task RemoveItemAsync(SchoolDictionaryKind kind, int id)
        {
            await _permissionService.RequirePermissionAsync(EnumPermissions.ManageSchoolData);
            ValidateKind(kind);
            if (id <= 0) return;

            var usage = await _repository.CountUsageAsync(kind, id);
            if (usage > 0)
            {
                throw new InvalidOperationException(
                    $"Nie można usunąć: pozycja jest użyta w zastępstwach ({usage}). Najpierw usuń lub zmień te zastępstwa.");
            }

            if (!await _repository.RemoveItemAsync(kind, id))
            {
                throw new InvalidOperationException("Nie znaleziono pozycji. Odśwież listę.");
            }
            _logger.LogInformation("Usunięto pozycję słownika {Kind} {ItemId} (soft delete).", kind, id);
        }

        private static void ValidateKind(SchoolDictionaryKind kind)
        {
            if (!Enum.IsDefined(kind))
            {
                throw new InvalidOperationException("Nieprawidłowy rodzaj danych.");
            }
        }

        // Przycina spacje (także podwójne w środku) i pilnuje długości.
        private static string CleanName(SchoolDictionaryKind kind, string? name)
        {
            var clean = string.Join(' ', (name ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            if (clean.Length == 0)
            {
                throw new InvalidOperationException($"Uzupełnij pole „{kind.NameLabel()}”.");
            }

            if (clean.Length > SchoolDictionaryItem.MaxNameLength)
            {
                throw new InvalidOperationException(
                    $"Pole „{kind.NameLabel()}” może mieć maksymalnie {SchoolDictionaryItem.MaxNameLength} znaków.");
            }

            return clean;
        }
    }
}

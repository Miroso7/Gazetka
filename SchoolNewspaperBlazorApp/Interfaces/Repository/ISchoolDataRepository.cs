using SchoolNewspaperBlazorApp.Data;
using SchoolNewspaperBlazorApp.Data.Enums;

namespace SchoolNewspaperBlazorApp.Interfaces.Repository
{
    public interface ISchoolDataRepository
    {
        // Pozycje słownika posortowane alfabetycznie.
        Task<List<SchoolDictionaryItem>> GetItemsAsync(SchoolDictionaryKind kind);
        Task<bool> ExistsAsync(SchoolDictionaryKind kind, int id);
        // Czy jest już pozycja o takiej nazwie (bez rozróżniania wielkości liter); excludeId pomija edytowaną pozycję.
        Task<bool> NameExistsAsync(SchoolDictionaryKind kind, string name, int? excludeId = null);
        Task AddItemAsync(SchoolDictionaryKind kind, string name);
        // Zwraca false, gdy pozycja nie istnieje.
        Task<bool> UpdateItemAsync(SchoolDictionaryKind kind, int id, string name);
        // Usuwa pozycję miękko (IsDeleted = 1). Zwraca false, gdy pozycja nie istnieje.
        Task<bool> RemoveItemAsync(SchoolDictionaryKind kind, int id);
        // Liczba zastępstw (nieusuniętych), w których użyto pozycji.
        Task<int> CountUsageAsync(SchoolDictionaryKind kind, int id);
    }
}

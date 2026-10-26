using SchoolNewspaperBlazorApp.Data;
using SchoolNewspaperBlazorApp.Data.Enums;

namespace SchoolNewspaperBlazorApp.Interfaces.Service
{
    public interface ISchoolDataService
    {
        // Odczyt słowników nie wymaga uprawnień (te same dane są na publicznej stronie zastępstw).
        Task<List<SchoolDictionaryItem>> GetItemsAsync(SchoolDictionaryKind kind);

        // Trzy poniższe wymagają permisji ManageSchoolData. Błędy walidacji to InvalidOperationException z komunikatem dla użytkownika.
        Task AddItemAsync(SchoolDictionaryKind kind, string name);
        Task UpdateItemAsync(SchoolDictionaryKind kind, int id, string name);
        // Nie usuwa pozycji użytej w zastępstwach.
        Task RemoveItemAsync(SchoolDictionaryKind kind, int id);
    }
}

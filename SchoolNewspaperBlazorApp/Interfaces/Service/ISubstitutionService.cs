using SchoolNewspaperBlazorApp.Data;

namespace SchoolNewspaperBlazorApp.Interfaces.Service
{
    public interface ISubstitutionService
    {
        // Odczyt jest publiczny. date = null: wszystkie zastępstwa.
        Task<List<Substitution>> GetSubstitutionsAsync(DateTime? date);

        // Wymagają permisji AddSubstitution / EditSubstitution / DeleteSubstitution.
        // Błędy walidacji to InvalidOperationException z komunikatem dla użytkownika.
        Task AddSubstitutionAsync(SubstitutionInput input);
        Task UpdateSubstitutionAsync(SubstitutionInput input);
        Task RemoveSubstitutionAsync(int id);
    }
}

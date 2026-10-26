using SchoolNewspaperBlazorApp.Data;

namespace SchoolNewspaperBlazorApp.Interfaces.Repository
{
    public interface ISubstitutionRepository
    {
        // date = null: wszystkie zastępstwa. Wynik z nawigacjami, posortowany: najpierw dzień (od najnowszego), potem lekcja.
        Task<List<Substitution>> GetSubstitutionsAsync(DateTime? date);
        Task AddSubstitutionAsync(Substitution substitution);
        // Zwraca false, gdy zastępstwo nie istnieje.
        Task<bool> UpdateSubstitutionAsync(Substitution substitution);
        // Usuwa miękko (IsDeleted = 1). Zwraca false, gdy zastępstwo nie istnieje.
        Task<bool> RemoveSubstitutionAsync(int id);
        // Czy klasa ma już zastępstwo w tym dniu na tej lekcji (excludeId pomija edytowane zastępstwo).
        Task<bool> LessonTakenAsync(DateTime date, int lesson, int classId, int? excludeId = null);
    }
}

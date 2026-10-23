namespace SchoolNewspaperBlazorApp.Data
{
    // Encja usuwana "miękko": wiersz zostaje w bazie, a usunięcie ustawia tylko IsDeleted = 1 (i DeletedAt).
    // Zapytania EF pomijają takie wiersze (globalne filtry w NewspaperDbContext), a context.Remove(...)
    // zamienia się w UPDATE (patrz NewspaperDbContext.ApplySoftDelete).
    // Przywrócenie: ręcznie w SQL (UPDATE ... SET IsDeleted = 0, DeletedAt = NULL).
    public interface ISoftDelete
    {
        bool IsDeleted { get; set; }

        // moment usunięcia (UTC); null, gdy wiersz nie jest usunięty
        DateTime? DeletedAt { get; set; }
    }
}

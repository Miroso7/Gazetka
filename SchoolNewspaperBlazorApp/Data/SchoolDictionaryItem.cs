namespace SchoolNewspaperBlazorApp.Data
{
    // Wspólna baza słowników szkoły (klasy, nauczyciele, sale, przedmioty). Sama nie ma tabeli,
    // jej pola trafiają do tabeli każdej klasy pochodnej. Usuwanie jest miękkie (IsDeleted = 1).
    public abstract class SchoolDictionaryItem : ISoftDelete
    {
        public const int MaxNameLength = 100;

        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
    }

    // Klasa szkolna, np. 3A.
    public class SchoolClass : SchoolDictionaryItem { }

    // Nauczyciel: imię i nazwisko w jednym polu.
    public class Teacher : SchoolDictionaryItem { }

    // Sala lekcyjna, np. 12 albo „Pracownia 3”.
    public class Room : SchoolDictionaryItem { }

    // Przedmiot szkolny, np. matematyka.
    public class Subject : SchoolDictionaryItem { }
}

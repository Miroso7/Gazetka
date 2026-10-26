namespace SchoolNewspaperBlazorApp.Data.Enums
{
    // Rodzaj słownika danych szkoły (zakładki w panelu admina).
    public enum SchoolDictionaryKind
    {
        Class = 1,
        Teacher = 2,
        Room = 3,
        Subject = 4,
    }

    public static class SchoolDictionaryKindExtensions
    {
        // liczba mnoga, np. nazwa zakładki
        public static string Label(this SchoolDictionaryKind kind) => kind switch
        {
            SchoolDictionaryKind.Class => "Klasy",
            SchoolDictionaryKind.Teacher => "Nauczyciele",
            SchoolDictionaryKind.Room => "Sale",
            SchoolDictionaryKind.Subject => "Przedmioty",
            _ => ""
        };

        // liczba pojedyncza w dopełniaczu, np. „Usunąć klasę?”
        public static string SingularAccusative(this SchoolDictionaryKind kind) => kind switch
        {
            SchoolDictionaryKind.Class => "klasę",
            SchoolDictionaryKind.Teacher => "nauczyciela",
            SchoolDictionaryKind.Room => "salę",
            SchoolDictionaryKind.Subject => "przedmiot",
            _ => "pozycję"
        };

        public static string NameLabel(this SchoolDictionaryKind kind) => kind switch
        {
            SchoolDictionaryKind.Class => "Nazwa klasy",
            SchoolDictionaryKind.Teacher => "Imię i nazwisko",
            SchoolDictionaryKind.Room => "Numer lub nazwa sali",
            SchoolDictionaryKind.Subject => "Nazwa przedmiotu",
            _ => "Nazwa"
        };

        public static string Placeholder(this SchoolDictionaryKind kind) => kind switch
        {
            SchoolDictionaryKind.Class => "np. 3A",
            SchoolDictionaryKind.Teacher => "np. Jan Kowalski",
            SchoolDictionaryKind.Room => "np. 12",
            SchoolDictionaryKind.Subject => "np. matematyka",
            _ => ""
        };

        public static string EmptyText(this SchoolDictionaryKind kind) => kind switch
        {
            SchoolDictionaryKind.Class => "Nie dodano jeszcze żadnych klas.",
            SchoolDictionaryKind.Teacher => "Nie dodano jeszcze żadnych nauczycieli.",
            SchoolDictionaryKind.Room => "Nie dodano jeszcze żadnych sal.",
            SchoolDictionaryKind.Subject => "Nie dodano jeszcze żadnych przedmiotów.",
            _ => ""
        };
    }
}

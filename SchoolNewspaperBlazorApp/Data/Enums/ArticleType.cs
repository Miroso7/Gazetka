namespace SchoolNewspaperBlazorApp.Data.Enums
{
    // Wartości są zapisywane w bazie jako liczby – nie zmieniaj istniejących numerów.
    public enum ArticleType
    {
        News = 1,
        Events = 2,
        Announcements = 3,
        Substitute = 4,
        Competitions = 5,
        Admissions = 6,
        Exams = 7,
        StudentCouncil = 8,
    }

    public static class ArticleTypeExtensions
    {
        public static string Label(this ArticleType type) => type switch
        {
            ArticleType.News => "Nowości",
            ArticleType.Events => "Wydarzenia",
            ArticleType.Announcements => "Ogłoszenia",
            ArticleType.Substitute => "Zastępstwa",
            ArticleType.Competitions => "Zawody",
            ArticleType.Admissions => "Rekrutacja",
            ArticleType.Exams => "Egzaminy",
            ArticleType.StudentCouncil => "Samorząd uczniowski",
            _ => "Artykuły"
        };

        public static string Description(this ArticleType type) => type switch
        {
            ArticleType.News => "Najnowsze wiadomości z życia szkoły",
            ArticleType.Events => "Konkursy, wycieczki, uroczystości i inne wydarzenia",
            ArticleType.Announcements => "Ważne komunikaty dla uczniów i rodziców",
            ArticleType.Substitute => "Aktualne zastępstwa za nieobecnych nauczycieli",
            ArticleType.Competitions => "Zawody sportowe i konkursy z udziałem naszych uczniów",
            ArticleType.Admissions => "Wszystko dla kandydatów: terminy, zasady i dokumenty",
            ArticleType.Exams => "Terminy, harmonogramy i informacje o egzaminach",
            ArticleType.StudentCouncil => "Wieści i inicjatywy samorządu uczniowskiego",
            _ => ""
        };

        public static string EmptyText(this ArticleType type) => type switch
        {
            ArticleType.News => "Nie ma jeszcze żadnych nowości.",
            ArticleType.Events => "Nie ma jeszcze żadnych wydarzeń.",
            ArticleType.Announcements => "Nie ma jeszcze żadnych ogłoszeń.",
            ArticleType.Substitute => "Nie ma jeszcze żadnych zastępstw.",
            ArticleType.Competitions => "Nie ma jeszcze żadnych zawodów.",
            ArticleType.Admissions => "Nie ma jeszcze informacji o rekrutacji.",
            ArticleType.Exams => "Nie ma jeszcze informacji o egzaminach.",
            ArticleType.StudentCouncil => "Nie ma jeszcze wpisów samorządu uczniowskiego.",
            _ => "Nie ma jeszcze żadnych artykułów."
        };

        // używane w nazwach klas CSS, np. article-card__type--events
        public static string CssName(this ArticleType type) => type.ToString().ToLowerInvariant();
    }
}

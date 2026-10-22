namespace SchoolNewspaperBlazorApp.Data.Enums
{
    // Wartości są zapisywane w bazie jako liczby – nie zmieniaj istniejących numerów.
    public enum ArticleType
    {
        News = 1,
        Events = 2,
        Announcements = 3,
    }

    public static class ArticleTypeExtensions
    {
        public static string Label(this ArticleType type) => type switch
        {
            ArticleType.News => "Nowości",
            ArticleType.Events => "Wydarzenia",
            ArticleType.Announcements => "Ogłoszenia",
            _ => "Artykuły"
        };

        public static string Description(this ArticleType type) => type switch
        {
            ArticleType.News => "Najnowsze wiadomości z życia szkoły",
            ArticleType.Events => "Konkursy, wycieczki, uroczystości i inne wydarzenia",
            ArticleType.Announcements => "Ważne komunikaty dla uczniów i rodziców",
            _ => ""
        };

        public static string EmptyText(this ArticleType type) => type switch
        {
            ArticleType.News => "Nie ma jeszcze żadnych nowości.",
            ArticleType.Events => "Nie ma jeszcze żadnych wydarzeń.",
            ArticleType.Announcements => "Nie ma jeszcze żadnych ogłoszeń.",
            _ => "Nie ma jeszcze żadnych artykułów."
        };

        // używane w nazwach klas CSS, np. article-card__type--events
        public static string CssName(this ArticleType type) => type.ToString().ToLowerInvariant();
    }
}

using SchoolNewspaperBlazorApp.Data.Enums;

namespace SchoolNewspaperBlazorApp.Data
{
    // Ułożenie zdjęcia z galerii w treści artykułu: gdzie się pokazuje i po którym akapicie (0 = przed pierwszym).
    // Order: miejsce zdjęcia w galerii (0 = pierwsze).
    public record ArticleImagePlacement(InlinePlacement Placement, int After, int Order = 0);
}

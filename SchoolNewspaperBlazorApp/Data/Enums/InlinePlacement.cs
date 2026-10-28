namespace SchoolNewspaperBlazorApp.Data.Enums
{
    // Gdzie zdjęcie z galerii pokazuje się na stronie artykułu.
    public enum InlinePlacement
    {
        // tylko w galerii pod artykułem
        Gallery = 0,
        // w treści, po lewej stronie (tekst obtacza zdjęcie)
        Left = 1,
        // w treści, na środku, na całą szerokość
        Center = 2,
        // w treści, po prawej stronie (tekst obtacza zdjęcie)
        Right = 3
    }
}

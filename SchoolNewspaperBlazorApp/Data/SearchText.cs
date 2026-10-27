using System.Globalization;
using System.Text;

namespace SchoolNewspaperBlazorApp.Data
{
    // Wspólne zasady wyszukiwania (strona główna i zastępstwa): bez rozróżniania wielkości liter i polskich znaków,
    // a każde wpisane słowo musi wystąpić w przeszukiwanym tekście.
    public static class SearchText
    {
        public const int MaxLength = 100;

        // czyści frazę z adresu: przycina spacje i ogranicza długość
        public static string Clean(string? term)
        {
            var value = (term ?? "").Trim();
            return value.Length > MaxLength ? value[..MaxLength].TrimEnd() : value;
        }

        // małe litery i bez znaków diakrytycznych: „zastepstwa” znajdzie też „Zastępstwa”
        public static string Fold(string? value)
        {
            var decomposed = (value ?? "").Replace('ł', 'l').Replace('Ł', 'L').Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(decomposed.Length);
            foreach (var c in decomposed)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
            }
            return sb.ToString().ToLowerInvariant();
        }

        public static string[] Words(string? term) =>
            Fold(term).Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // haystack to dowolny tekst do przeszukania (zostanie sprowadzony tak samo jak fraza)
        public static bool MatchesAll(string[] words, string? haystack)
        {
            var folded = Fold(haystack);
            return words.All(word => folded.Contains(word));
        }
    }
}

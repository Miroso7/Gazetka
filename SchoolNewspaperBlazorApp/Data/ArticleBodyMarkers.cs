using System.Text.RegularExpressions;

namespace SchoolNewspaperBlazorApp.Data
{
    // Fragment treści artykułu: akapit tekstu albo zdjęcie z galerii (numer od 1).
    public record ArticleBodyPart(string? Text, int? Photo);

    // Zdjęcia w środku tekstu: akapit złożony wyłącznie ze znacznika [foto:N] zamienia się na zdjęcie nr N z galerii artykułu.
    // Dzięki temu nie trzeba zmieniać bazy – znacznik jest zwykłym tekstem w treści.
    public static class ArticleBodyMarkers
    {
        private static readonly Regex WholeParagraph = new(@"^\[foto:(\d+)\]$", RegexOptions.Compiled);
        private static readonly Regex AnyMarker = new(@"\[foto:(\d+)\]", RegexOptions.Compiled);

        public static string Marker(int number) => $"[foto:{number}]";

        public static IReadOnlyList<ArticleBodyPart> Split(string? text)
        {
            var normalized = (text ?? "").Replace("\r\n", "\n").Trim();
            return normalized
                .Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(paragraph =>
                {
                    var match = WholeParagraph.Match(paragraph);
                    return match.Success && int.TryParse(match.Groups[1].Value, out var number)
                        ? new ArticleBodyPart(null, number)
                        : new ArticleBodyPart(paragraph, null);
                })
                .ToList();
        }

        // numery zdjęć wstawionych w treść
        public static HashSet<int> UsedPhotos(IEnumerable<ArticleBodyPart> parts) =>
            parts.Where(p => p.Photo is not null).Select(p => p.Photo!.Value).ToHashSet();

        // po usunięciu zdjęcia z galerii znika jego znacznik, a kolejne zdjęcia dostają numer o 1 mniejszy
        public static string OnPhotoRemoved(string? text, int removedNumber) =>
            AnyMarker.Replace(text ?? "", match =>
            {
                var number = int.Parse(match.Groups[1].Value);
                if (number == removedNumber) return "";
                return number > removedNumber ? Marker(number - 1) : match.Value;
            });
    }
}

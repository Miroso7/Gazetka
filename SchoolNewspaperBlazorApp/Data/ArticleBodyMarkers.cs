using System.Text.RegularExpressions;
using SchoolNewspaperBlazorApp.Data.Enums;

namespace SchoolNewspaperBlazorApp.Data
{
    // Zdjęcie z galerii artykułu użyte przy układaniu treści: adres i ułożenie (patrz ArticleImage).
    public record ArticleBodyPhoto(string Url, InlinePlacement Placement, int After);

    // Fragment treści artykułu: akapit tekstu albo zdjęcie z galerii (numer od 1; tylko dla starych znaczników).
    public record ArticleBodyPart(string? Text, int? Photo, InlinePlacement Placement = InlinePlacement.Gallery);

    // Gotowy blok do wyświetlenia: akapit albo zdjęcie (indeks od 0 w liście zdjęć) z miejscem.
    public record ArticleBodyBlock(string? Text, int? Photo, InlinePlacement Placement);

    // Zdjęcia w treści układa się teraz w formularzu (zapis w ArticleImage), więc treść artykułu jest zwykłym tekstem.
    // Obsługujemy jeszcze stare znaczniki [foto:N] (także z dopiskiem :lewo, :srodek, :prawo), żeby dawne artykuły
    // wyglądały jak dotąd; po otwarciu takiego artykułu do edycji znaczniki zamieniają się na ułożenie zdjęć.
    public static class ArticleBodyMarkers
    {
        private static readonly Regex WholeParagraph = new(@"^\[foto:(\d+)(?::(lewo|srodek|prawo))?\]$", RegexOptions.Compiled);
        private static readonly Regex AnyMarker = new(@"\[foto:\d+(?::(?:lewo|srodek|prawo))?\]", RegexOptions.Compiled);

        public static IReadOnlyList<ArticleBodyPart> Split(string? text)
        {
            var normalized = (text ?? "").Replace("\r\n", "\n").Trim();
            return normalized
                .Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(paragraph =>
                {
                    var match = WholeParagraph.Match(paragraph);
                    return match.Success && int.TryParse(match.Groups[1].Value, out var number)
                        ? new ArticleBodyPart(null, number, match.Groups[2].Value switch
                        {
                            "lewo" => InlinePlacement.Left,
                            "prawo" => InlinePlacement.Right,
                            _ => InlinePlacement.Center
                        })
                        : new ArticleBodyPart(paragraph, null);
                })
                .ToList();
        }

        // tekst bez starych znaczników zdjęć (do list, wyszukiwania i liczenia słów)
        public static string StripMarkers(string? text) => AnyMarker.Replace(text ?? "", " ");

        // Układa treść artykułu: akapity i zdjęcia we właściwych miejscach.
        // photos to zdjęcia galerii w kolejności numeracji; zdjęcie z miejscem „Gallery” nie trafia do treści.
        public static IReadOnlyList<ArticleBodyBlock> Layout(string? text, IReadOnlyList<ArticleBodyPhoto> photos)
        {
            var parts = Split(text);

            // zdjęcia wskazane starymi znacznikami zostają tam, gdzie je ustawiono
            var legacy = parts
                .Where(p => p.Photo is int n && n >= 1 && n <= photos.Count)
                .Select(p => p.Photo!.Value - 1)
                .ToHashSet();

            var paragraphCount = parts.Count(p => p.Text is not null);
            var slots = new Dictionary<int, List<int>>();
            for (var i = 0; i < photos.Count; i++)
            {
                if (photos[i].Placement == InlinePlacement.Gallery || legacy.Contains(i)) continue;

                // numer akapitu większy niż liczba akapitów (np. po skróceniu tekstu) kładzie zdjęcie na końcu
                var slot = Math.Clamp(photos[i].After, 0, paragraphCount);
                if (!slots.TryGetValue(slot, out var list)) slots[slot] = list = new List<int>();
                list.Add(i);
            }

            var blocks = new List<ArticleBodyBlock>();
            void AddSlot(int slot)
            {
                if (!slots.TryGetValue(slot, out var list)) return;
                blocks.AddRange(list.Select(i => new ArticleBodyBlock(null, i, photos[i].Placement)));
            }

            AddSlot(0);
            var written = 0;
            foreach (var part in parts)
            {
                if (part.Text is not null)
                {
                    blocks.Add(new ArticleBodyBlock(part.Text, null, InlinePlacement.Gallery));
                    written++;
                    AddSlot(written);
                }
                else if (part.Photo is int number && number >= 1 && number <= photos.Count)
                {
                    // znacznik wskazujący nieistniejące zdjęcie jest po prostu pomijany
                    blocks.Add(new ArticleBodyBlock(null, number - 1, part.Placement));
                }
            }
            return blocks;
        }

        // Zamienia stare znaczniki na ułożenie zdjęć: zwraca czysty tekst i miejsca zdjęć (indeks od 0 -> ułożenie).
        public static string ExtractLegacyMarkers(string? text, int photoCount, out Dictionary<int, ArticleImagePlacement> placements)
        {
            placements = new Dictionary<int, ArticleImagePlacement>();
            var clean = new List<string>();
            foreach (var part in Split(text))
            {
                if (part.Text is not null)
                {
                    clean.Add(part.Text);
                }
                else if (part.Photo is int number && number >= 1 && number <= photoCount)
                {
                    placements.TryAdd(number - 1, new ArticleImagePlacement(part.Placement, clean.Count));
                }
            }
            return string.Join("\n\n", clean);
        }
    }
}

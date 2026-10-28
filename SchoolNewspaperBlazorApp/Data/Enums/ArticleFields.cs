using System.Globalization;
using System.Text.Json;
using SchoolNewspaperBlazorApp.Data.Enums;

namespace SchoolNewspaperBlazorApp.Data
{
    public enum ArticleFieldKind
    {
        Text,
        Date,
        DateTime,
        Number,
        Select,
        Checkbox
    }

    // Opis jednego dodatkowego pola formularza artykułu (zależnego od typu artykułu).
    public record ArticleField(
        string Key,
        string Label,
        ArticleFieldKind Kind = ArticleFieldKind.Text,
        bool Required = false,
        string? Placeholder = null,
        string[]? Options = null)
    {
        // typ pola <input> w HTML
        public string InputType => Kind switch
        {
            ArticleFieldKind.Date => "date",
            ArticleFieldKind.DateTime => "datetime-local",
            ArticleFieldKind.Number => "number",
            _ => "text"
        };
    }

    // Pola formularza dla każdego typu artykułu. Wartości trafiają do Article.Details jako JSON,
    // więc dodanie albo zmiana pola nie wymaga migracji bazy.
    public static class ArticleFields
    {
        private const int MaxTextLength = 200;
        private const string DateFormat = "yyyy-MM-dd";
        private const string DateTimeFormat = "yyyy-MM-ddTHH:mm";

        public static IReadOnlyList<ArticleField> For(ArticleType type) => type switch
        {
            ArticleType.News => new ArticleField[]
            {
                new("source", "Źródło informacji", Placeholder: "np. rozmowa z dyrekcją"),
                new("photoAuthor", "Autor zdjęć", Placeholder: "Imię i nazwisko"),
            },
            ArticleType.Events => new ArticleField[]
            {
                new("date", "Data i godzina", ArticleFieldKind.DateTime, Required: true),
                new("place", "Miejsce", Placeholder: "np. aula, boisko szkolne"),
                new("organizer", "Organizator", Placeholder: "np. samorząd uczniowski"),
                new("entry", "Wstęp", ArticleFieldKind.Select, Options: new[] { "Wstęp wolny", "Wymagane zapisy", "Bilety" }),
            },
            ArticleType.Announcements => new ArticleField[]
            {
                new("validUntil", "Ważne do", ArticleFieldKind.Date),
                new("audience", "Adresaci", ArticleFieldKind.Select, Options: new[] { "Wszyscy", "Uczniowie", "Rodzice", "Nauczyciele" }),
                new("urgent", "Pilne ogłoszenie", ArticleFieldKind.Checkbox),
            },
            ArticleType.Substitute => new ArticleField[]
            {
                new("date", "Dzień zastępstw", ArticleFieldKind.Date, Required: true),
                new("class", "Klasa", Placeholder: "np. 3A"),
                new("absentTeacher", "Nieobecny nauczyciel", Placeholder: "Imię i nazwisko"),
                new("substituteTeacher", "Zastępujący nauczyciel", Placeholder: "Imię i nazwisko"),
                new("room", "Sala", Placeholder: "np. 12"),
            },
            ArticleType.Competitions => new ArticleField[]
            {
                new("discipline", "Dyscyplina lub konkurs", Required: true, Placeholder: "np. piłka siatkowa"),
                new("date", "Data", ArticleFieldKind.Date),
                new("place", "Miejsce", Placeholder: "np. hala sportowa"),
                new("result", "Wynik lub zajęte miejsce", Placeholder: "np. 2. miejsce"),
            },
            ArticleType.Admissions => new ArticleField[]
            {
                new("schoolYear", "Rok szkolny", Required: true, Placeholder: "np. 2026/2027"),
                new("field", "Kierunek", Placeholder: "np. technik programista"),
                new("deadline", "Termin składania dokumentów", ArticleFieldKind.Date),
                new("places", "Liczba miejsc", ArticleFieldKind.Number, Placeholder: "np. 30"),
            },
            ArticleType.Exams => new ArticleField[]
            {
                new("kind", "Rodzaj egzaminu", ArticleFieldKind.Select, Required: true,
                    Options: new[] { "Matura", "Egzamin zawodowy", "Egzamin ósmoklasisty", "Egzamin poprawkowy", "Inny" }),
                new("subject", "Przedmiot", Placeholder: "np. matematyka"),
                new("date", "Termin", ArticleFieldKind.DateTime, Required: true),
                new("room", "Sala", Placeholder: "np. 12"),
            },
            ArticleType.StudentCouncil => new ArticleField[]
            {
                new("kind", "Rodzaj wpisu", ArticleFieldKind.Select, Required: true,
                    Options: new[] { "Spotkanie", "Akcja", "Głosowanie", "Komunikat" }),
                new("date", "Data", ArticleFieldKind.Date),
                new("contact", "Kontakt", Placeholder: "np. e-mail samorządu"),
            },
            _ => Array.Empty<ArticleField>()
        };

        // Odczyt zapisanych pól (pusty słownik, gdy brak danych lub JSON jest uszkodzony).
        public static Dictionary<string, string> Parse(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new Dictionary<string, string>();
            }

            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                       ?? new Dictionary<string, string>();
            }
            catch (JsonException)
            {
                return new Dictionary<string, string>();
            }
        }

        // Sprawdza pola formularza dla danego typu i zwraca JSON do zapisu (null, gdy nic nie wpisano).
        // Pomija pola z innych typów, przycina spacje i rzuca InvalidOperationException z komunikatem dla użytkownika.
        public static string? ToJson(ArticleType type, IReadOnlyDictionary<string, string>? values)
        {
            var clean = new Dictionary<string, string>();

            foreach (var field in For(type))
            {
                string? value = null;
                values?.TryGetValue(field.Key, out value);
                value = value?.Trim();

                if (field.Kind == ArticleFieldKind.Checkbox)
                {
                    if (value == "true")
                    {
                        clean[field.Key] = "true";
                    }
                    continue;
                }

                if (string.IsNullOrEmpty(value))
                {
                    if (field.Required)
                    {
                        throw new InvalidOperationException($"Uzupełnij pole „{field.Label}”.");
                    }
                    continue;
                }

                switch (field.Kind)
                {
                    case ArticleFieldKind.Text when value.Length > MaxTextLength:
                        throw new InvalidOperationException(
                            $"Pole „{field.Label}” może mieć maksymalnie {MaxTextLength} znaków.");

                    case ArticleFieldKind.Date when !TryParseDate(value, DateFormat, out _):
                    case ArticleFieldKind.DateTime when !TryParseDate(value, DateTimeFormat, out _):
                        throw new InvalidOperationException($"Pole „{field.Label}” ma nieprawidłową datę.");

                    case ArticleFieldKind.Number when !int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var number) || number > 100_000:
                        throw new InvalidOperationException($"Pole „{field.Label}” musi być liczbą całkowitą.");

                    case ArticleFieldKind.Select when field.Options is null || !field.Options.Contains(value):
                        throw new InvalidOperationException($"Wybierz poprawną wartość w polu „{field.Label}”.");
                }

                clean[field.Key] = value;
            }

            return clean.Count == 0 ? null : JsonSerializer.Serialize(clean);
        }

        // Odmiana dla wersji roboczych: nie sprawdza wymaganych pól ani formatów (wersja robocza może być niedokończona).
        // Przy publikacji i tak działa ToJson.
        public static string? ToDraftJson(ArticleType type, IReadOnlyDictionary<string, string>? values)
        {
            var clean = new Dictionary<string, string>();

            foreach (var field in For(type))
            {
                string? value = null;
                values?.TryGetValue(field.Key, out value);
                value = value?.Trim();
                if (string.IsNullOrEmpty(value)) continue;

                clean[field.Key] = value.Length > MaxTextLength ? value[..MaxTextLength] : value;
            }

            return clean.Count == 0 ? null : JsonSerializer.Serialize(clean);
        }

        // Wypełnione pola artykułu w kolejności z formularza, z wartościami gotowymi do pokazania.
        public static List<(string Label, string Value)> Display(Article article)
        {
            var stored = Parse(article.Details);
            var result = new List<(string, string)>();

            foreach (var field in For(article.Type))
            {
                if (!stored.TryGetValue(field.Key, out var value) || string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                result.Add((field.Label, Format(field, value)));
            }

            return result;
        }

        private static string Format(ArticleField field, string value)
        {
            switch (field.Kind)
            {
                case ArticleFieldKind.Date when TryParseDate(value, DateFormat, out var date):
                    return date.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
                case ArticleFieldKind.DateTime when TryParseDate(value, DateTimeFormat, out var dateTime):
                    return dateTime.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
                case ArticleFieldKind.Checkbox:
                    return "Tak";
                default:
                    return value;
            }
        }

        private static bool TryParseDate(string value, string format, out DateTime result) =>
            DateTime.TryParseExact(value, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
    }
}

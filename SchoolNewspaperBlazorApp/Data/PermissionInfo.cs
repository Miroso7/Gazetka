using SchoolNewspaperBlazorApp.Data.Enums;

namespace SchoolNewspaperBlazorApp.Data
{
    // Polskie nazwy i opisy permisji wyświetlane w panelu admina.
    // Nowa permisja = nowa wartość na KOŃCU enuma EnumPermissions + wpis tutaj.
    public static class PermissionInfo
    {
        // Uprawnienia, które można nadawać z panelu. Master (i wynikające z niego ManageUser)
        // nadaje się wyłącznie ręcznie w bazie, więc nie pokazujemy ich jako pól do zaznaczenia.
        public static IReadOnlyList<EnumPermissions> All { get; } = Enum.GetValues<EnumPermissions>()
            .Where(p => p is not (EnumPermissions.Master
                               or EnumPermissions.ManageUser
                               or EnumPermissions.ManagePermissions))
            .ToList();

        public static string Label(EnumPermissions permission) => permission switch
        {
            EnumPermissions.AddArticle => "Dodawanie artykułów",
            EnumPermissions.EditArticle => "Edycja artykułów",
            EnumPermissions.DeleteArticle => "Usuwanie artykułów",
            EnumPermissions.AddUser => "Dodawanie użytkowników",
            EnumPermissions.ManageUser => "Zarządzanie uprawnieniami",
            EnumPermissions.Master => "Master",
            EnumPermissions.AddSubstitution => "Dodawanie zastępstw",
            EnumPermissions.EditSubstitution => "Edycja zastępstw",
            EnumPermissions.DeleteSubstitution => "Usuwanie zastępstw",
            EnumPermissions.ManageSchoolData => "Dane szkoły",
            _ => permission.ToString()
        };

        public static string Description(EnumPermissions permission) => permission switch
        {
            EnumPermissions.AddArticle => "Zakładka „Dodaj artykuł” i zapisywanie nowych artykułów.",
            EnumPermissions.EditArticle => "Przycisk „Edytuj” przy artykułach i zapisywanie zmian.",
            EnumPermissions.DeleteArticle => "Przycisk „Usuń” przy artykułach.",
            EnumPermissions.AddUser => "Zakładka „Dodaj użytkownika” i tworzenie nowych kont.",
            EnumPermissions.ManageUser => "Zakładka „Uprawnienia”: nadawanie i odbieranie permisji innym.",
            EnumPermissions.Master => "Konto główne: jako jedyne zarządza uprawnieniami innych użytkowników.",
            EnumPermissions.AddSubstitution => "Zakładka „Zastępstwa”: dodawanie nowych zastępstw.",
            EnumPermissions.EditSubstitution => "Przycisk „Edytuj” przy zastępstwach i zapisywanie zmian.",
            EnumPermissions.DeleteSubstitution => "Przycisk „Usuń” przy zastępstwach.",
            EnumPermissions.ManageSchoolData => "Zakładka „Dane szkoły”: klasy, nauczyciele, sale i przedmioty.",
            _ => ""
        };
    }

    // Użytkownik razem z jego permisjami (do widoku zarządzania).
    public class UserWithPermissions
    {
        public int UserId { get; set; }
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Email { get; set; } = "";
        public UserRole Role { get; set; } = UserRole.Student;
        public HashSet<EnumPermissions> Permissions { get; set; } = new();
        // nazwa pliku zdjęcia profilowego (null = brak zdjęcia, na liście pokazują się inicjały)
        public string? AvatarFileName { get; set; }
        public string? AvatarUrl => UserProfile.AvatarUrlFor(AvatarFileName);

        // konto master i konto zalogowanego użytkownika nie mogą zostać usunięte z panelu
        public bool IsMaster => Permissions.Contains(EnumPermissions.Master);
        public bool IsCurrentUser { get; set; }

        public string FullName => $"{FirstName} {LastName}".Trim();
        // imię i nazwisko, a gdy ich brak (starsze konta) – adres e-mail
        public string DisplayName => FullName.Length > 0 ? FullName : Email;
    }
}

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
            _ => ""
        };
    }

    // Użytkownik razem z jego permisjami (do widoku zarządzania).
    public class UserWithPermissions
    {
        public int UserId { get; set; }
        public string Login { get; set; } = "";
        public string Email { get; set; } = "";
        public HashSet<EnumPermissions> Permissions { get; set; } = new();
    }
}

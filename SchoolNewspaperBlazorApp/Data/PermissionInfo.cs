namespace SchoolNewspaperBlazorApp.Data
{
    // Polskie nazwy i opisy permisji wyświetlane w panelu admina.
    // Nowa permisja = nowa wartość na KOŃCU enuma EnumPermissions + wpis tutaj.
    public static class PermissionInfo
    {
        public static IReadOnlyList<EnumPermissions> All { get; } = Enum.GetValues<EnumPermissions>();

        public static string Label(EnumPermissions permission) => permission switch
        {
            EnumPermissions.AddArticle => "Dodawanie artykułów",
            EnumPermissions.EditArticle => "Edycja artykułów",
            EnumPermissions.DeleteArticle => "Usuwanie artykułów",
            EnumPermissions.AddUser => "Dodawanie użytkowników",
            EnumPermissions.ManageUser => "Zarządzanie uprawnieniami",
            _ => permission.ToString()
        };

        public static string Description(EnumPermissions permission) => permission switch
        {
            EnumPermissions.AddArticle => "Zakładka „Dodaj artykuł” i zapisywanie nowych artykułów.",
            EnumPermissions.EditArticle => "Przycisk „Edytuj” przy artykułach i zapisywanie zmian.",
            EnumPermissions.DeleteArticle => "Przycisk „Usuń” przy artykułach.",
            EnumPermissions.AddUser => "Zakładka „Dodaj użytkownika” i tworzenie nowych kont.",
            EnumPermissions.ManageUser => "Zakładka „Uprawnienia”: nadawanie i odbieranie permisji innym.",
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

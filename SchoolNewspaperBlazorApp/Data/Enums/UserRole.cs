namespace SchoolNewspaperBlazorApp.Data.Enums
{
    // Rola użytkownika w szkole (informacyjna). Uprawnienia w panelu nadaje się osobno (EnumPermissions).
    // Wartości są zapisywane w bazie jako liczby – nie zmieniaj istniejących numerów.
    public enum UserRole
    {
        Student = 1,
        Teacher = 2,
        Staff = 3,
    }

    public static class UserRoleExtensions
    {
        public static string Label(this UserRole role) => role switch
        {
            UserRole.Student => "Uczeń",
            UserRole.Teacher => "Nauczyciel",
            UserRole.Staff => "Pracownik szkoły",
            _ => "Brak roli"
        };
    }
}

using SchoolNewspaperBlazorApp.Data.Enums;

namespace SchoolNewspaperBlazorApp.Data
{
    // Użytkownik razem z listą jego uprawnień (do wyświetlenia w panelu).
    public class UserPermissionsDto
    {
        public int UserId { get; set; }
        public string Login { get; set; } = "";
        public string Email { get; set; } = "";
        public List<EnumPermissions> Permissions { get; set; } = new();
    }
}

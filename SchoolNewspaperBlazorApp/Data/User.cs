using SchoolNewspaperBlazorApp.Data.Enums;

namespace SchoolNewspaperBlazorApp.Data
{
    public class User : ISoftDelete
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public UserRole Role { get; set; } = UserRole.Student;
        public required string Password { get; set; }
        // true = przy następnym logowaniu użytkownik musi ustawić własne hasło (konto założone przez administratora)
        public bool MustChangePassword { get; set; }
        public required string Email { get; set; }
        public DateTime RegisterDate { get; set; }
        public string AddBy { get; set; } = string.Empty;
        public ICollection<UserPermission> UserPermission { get; set; } = new List<UserPermission>();

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}

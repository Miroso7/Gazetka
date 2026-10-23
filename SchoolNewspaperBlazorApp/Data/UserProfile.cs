using SchoolNewspaperBlazorApp.Data.Enums;

namespace SchoolNewspaperBlazorApp.Data
{
    // Dane konta bez hasła – do pokazywania i edycji w panelu.
    public class UserProfile
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Email { get; set; } = "";
        public UserRole Role { get; set; } = UserRole.Student;
    }
}

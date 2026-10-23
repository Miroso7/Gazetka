namespace SchoolNewspaperBlazorApp.Data
{
    public class User
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public required string Password { get; set; }
        public required string Email { get; set; }
        public DateTime RegisterDate { get; set; }
        public string AddBy { get; set; } = string.Empty;
        public ICollection<UserPermission> UserPermission { get; set; } = new List<UserPermission>();
    }
}

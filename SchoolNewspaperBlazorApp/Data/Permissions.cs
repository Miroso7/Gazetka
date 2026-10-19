namespace SchoolNewspaperBlazorApp.Data
{
    public class Permissions
    {
        public int Id { get; set; }
        public EnumPermissions PermissionName { get; set; }
        public ICollection<UserPermission> Permission { get; set; } = new List<UserPermission>();

    }
}

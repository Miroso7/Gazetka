using Microsoft.EntityFrameworkCore;

namespace SchoolNewspaperBlazorApp.Data
{
    public class NewspaperDbContext : DbContext
    {
        public NewspaperDbContext(DbContextOptions<NewspaperDbContext> options)
            : base(options) { }

        public DbSet<Article> Articles { get; set; }
        public DbSet<User> Users => Set<User>();
        public DbSet<MediaFile> Files { get; set; }
        public DbSet<Permission> Permissions => Set<Permission>();
        public DbSet<UserPermission> UserPermissions => Set<UserPermission>();

        protected override void OnModelCreating(ModelBuilder mb)
        {
            mb.Entity<MediaFile>()
                .Property(x => x.Id)
                .ValueGeneratedNever();

            // Słownik uprawnień: Id = numer z enuma, dane dodawane przez migrację.
            mb.Entity<Permission>(e =>
            {
                e.Property(x => x.Id).ValueGeneratedNever();
                e.HasIndex(x => x.PermissionName).IsUnique();
                e.HasData(Enum.GetValues<EnumPermissions>()
                    .Select(p => new Permission { Id = (int)p, PermissionName = p }));
            });

            mb.Entity<UserPermission>(e =>
            {
                e.HasIndex(x => new { x.UserId, x.PermissionId }).IsUnique();

                e.HasOne(x => x.User)
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.Permission)
                    .WithMany()
                    .HasForeignKey(x => x.PermissionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}

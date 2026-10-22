using Microsoft.EntityFrameworkCore;
using SchoolNewspaperBlazorApp.Data.Enums;

namespace SchoolNewspaperBlazorApp.Data
{
    public class NewspaperDbContext : DbContext
    {
        public NewspaperDbContext(DbContextOptions<NewspaperDbContext> options)
            : base(options) { }

        public DbSet<Article> Articles { get; set; }
        public DbSet<User> Users => Set<User>();
        public DbSet<MediaFile> Files { get; set; }
        public DbSet<ArticleImage> ArticleImages => Set<ArticleImage>();
        public DbSet<Permission> Permissions => Set<Permission>();
        public DbSet<UserPermission> UserPermissions => Set<UserPermission>();

        protected override void OnModelCreating(ModelBuilder mb)
        {
            mb.Entity<MediaFile>()
                .Property(x => x.Id)
                .ValueGeneratedNever();

            // Galeria: usunięcie artykułu usuwa też wpisy o jego zdjęciach (same pliki kasuje serwis).
            mb.Entity<ArticleImage>(e =>
            {
                e.HasIndex(x => x.ArticleId);

                e.HasOne<Article>()
                    .WithMany(a => a.Images)
                    .HasForeignKey(x => x.ArticleId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

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
                    .WithMany(x => x.UserPermission)
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

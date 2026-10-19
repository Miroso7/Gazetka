using Microsoft.EntityFrameworkCore;

namespace SchoolNewspaperBlazorApp.Data
{
    public class NewspaperDbContext : DbContext
    {
        public NewspaperDbContext(DbContextOptions<NewspaperDbContext> options)
            : base(options) {}
        public DbSet<Article> Articles { get; set; }
        public DbSet<User> Users => Set<User>();
        public DbSet<MediaFile> Files { get; set; }

        public DbSet<Permissions> Permissions { get; set; }
        public DbSet<UserPermission> UserPermissions { get; set; }
        protected override void OnModelCreating(ModelBuilder mb)
        {
            mb.Entity<MediaFile>()
                .Property(x => x.Id)
                .ValueGeneratedNever();
            mb.Entity<UserPermission>()
                .HasKey(up => new { up.UserId, up.PermissionId });

            mb.Entity<UserPermission>()
                .HasOne(up => up.User)
                .WithMany(u => u.UserPermission)
                .HasForeignKey(up => up.UserId);
            mb.Entity<UserPermission>()
                .HasOne(up => up.Permission)
                .WithMany(p => p.Permission)
                .HasForeignKey(up => up.PermissionId);

        }

        
    }

}

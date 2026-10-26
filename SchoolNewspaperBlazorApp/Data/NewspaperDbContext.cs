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
        public DbSet<SchoolClass> SchoolClasses => Set<SchoolClass>();
        public DbSet<Teacher> Teachers => Set<Teacher>();
        public DbSet<Room> Rooms => Set<Room>();
        public DbSet<Subject> Subjects => Set<Subject>();
        public DbSet<Substitution> Substitutions => Set<Substitution>();

        // Soft delete: usunięcie encji ISoftDelete nie kasuje wiersza, tylko ustawia IsDeleted = 1.
        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            ApplySoftDelete();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            ApplySoftDelete();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        // Zamienia DELETE na UPDATE: wpis w stanie Deleted wraca do Unchanged, a zapisują się tylko IsDeleted i DeletedAt.
        // Dotyczy też zależnych wpisów usuwanych kaskadowo przez śledzenie zmian EF.
        private void ApplySoftDelete()
        {
            var now = DateTime.UtcNow;
            var deleted = ChangeTracker.Entries<ISoftDelete>()
                .Where(e => e.State == EntityState.Deleted)
                .ToList();

            foreach (var entry in deleted)
            {
                entry.State = EntityState.Unchanged;
                entry.Property(nameof(ISoftDelete.IsDeleted)).CurrentValue = true;
                entry.Property(nameof(ISoftDelete.DeletedAt)).CurrentValue = now;
            }
        }

        protected override void OnModelCreating(ModelBuilder mb)
        {
            // Globalne filtry: usunięte wiersze znikają ze wszystkich zapytań.
            // Wyjątek wymaga jawnego .IgnoreQueryFilters() (np. wyznaczanie nowego Id pliku, przywracanie uprawnienia).
            mb.Entity<Article>().HasQueryFilter(a => !a.IsDeleted);
            mb.Entity<User>().HasQueryFilter(u => !u.IsDeleted);
            mb.Entity<MediaFile>().HasQueryFilter(f => !f.IsDeleted);
            mb.Entity<ArticleImage>().HasQueryFilter(i => !i.IsDeleted);
            // uprawnienia usuniętego użytkownika też są niewidoczne (jednocześnie spełnia to wymóg EF dla wymaganej relacji z User)
            mb.Entity<UserPermission>().HasQueryFilter(up => !up.IsDeleted && !up.User.IsDeleted);

            mb.Entity<SchoolClass>().HasQueryFilter(x => !x.IsDeleted);
            mb.Entity<Teacher>().HasQueryFilter(x => !x.IsDeleted);
            mb.Entity<Room>().HasQueryFilter(x => !x.IsDeleted);
            mb.Entity<Subject>().HasQueryFilter(x => !x.IsDeleted);
            mb.Entity<Substitution>().HasQueryFilter(x => !x.IsDeleted);

            mb.Entity<MediaFile>()
                .Property(x => x.Id)
                .ValueGeneratedNever();

            // Galeria: wpisy o zdjęciach artykułu (usunięcie artykułu jest miękkie, więc galeria zostaje do ewentualnego przywrócenia).
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

            // Słowniki szkoły: nazwa ma limit długości (pola z klasy bazowej SchoolDictionaryItem).
            mb.Entity<SchoolClass>().Property(x => x.Name).HasMaxLength(SchoolDictionaryItem.MaxNameLength);
            mb.Entity<Teacher>().Property(x => x.Name).HasMaxLength(SchoolDictionaryItem.MaxNameLength);
            mb.Entity<Room>().Property(x => x.Name).HasMaxLength(SchoolDictionaryItem.MaxNameLength);
            mb.Entity<Subject>().Property(x => x.Name).HasMaxLength(SchoolDictionaryItem.MaxNameLength);

            // Zastępstwa: słowniki usuwa się miękko, więc relacje są opcjonalne i bez kaskad (Restrict).
            mb.Entity<Substitution>(e =>
            {
                e.Property(x => x.Date).HasColumnType("date");
                e.Property(x => x.Note).HasMaxLength(Substitution.MaxNoteLength);
                e.HasIndex(x => x.Date);

                e.HasOne(x => x.Class).WithMany().HasForeignKey(x => x.ClassId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.Subject).WithMany().HasForeignKey(x => x.SubjectId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.AbsentTeacher).WithMany().HasForeignKey(x => x.AbsentTeacherId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.SubstituteTeacher).WithMany().HasForeignKey(x => x.SubstituteTeacherId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.Room).WithMany().HasForeignKey(x => x.RoomId).OnDelete(DeleteBehavior.Restrict);
            });

        }
    }
}

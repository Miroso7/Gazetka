using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SchoolNewspaperBlazorApp.Data;
using SchoolNewspaperBlazorApp.Interfaces.Repository;

namespace SchoolNewspaperBlazorApp.Repository
{
    public class SubstitutionRepository : ISubstitutionRepository
    {
        private readonly IDbContextFactory<NewspaperDbContext> _contextFactory;
        private readonly ILogger<SubstitutionRepository> _logger;

        public SubstitutionRepository(IDbContextFactory<NewspaperDbContext> contextFactory, ILogger<SubstitutionRepository> logger)
        {
            _contextFactory = contextFactory;
            _logger = logger;
        }

        public async Task<List<Substitution>> GetSubstitutionsAsync(DateTime? date)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var query = context.Substitutions
                .AsNoTracking()
                .Include(s => s.Class)
                .Include(s => s.Subject)
                .Include(s => s.AbsentTeacher)
                .Include(s => s.SubstituteTeacher)
                .Include(s => s.Room)
                .AsQueryable();

            if (date is { } value)
            {
                var day = value.Date;
                query = query.Where(s => s.Date == day);
            }

            return await query
                .OrderByDescending(s => s.Date)
                .ThenBy(s => s.Lesson)
                .ThenBy(s => s.Class!.Name)
                .ToListAsync();
        }

        public async Task AddSubstitutionAsync(Substitution substitution)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            await context.Substitutions.AddAsync(substitution);
            await context.SaveChangesAsync();
            _logger.LogDebug("Zapisano zastępstwo {SubstitutionId} w bazie.", substitution.Id);
        }

        public async Task<bool> UpdateSubstitutionAsync(Substitution substitution)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var existing = await context.Substitutions.FirstOrDefaultAsync(s => s.Id == substitution.Id);
            if (existing == null)
            {
                _logger.LogWarning("Nie można zaktualizować zastępstwa {SubstitutionId}: nie istnieje.", substitution.Id);
                return false;
            }

            // aktualizujemy tylko edytowalne pola (autor i data dodania zostają)
            existing.Date = substitution.Date;
            existing.Lesson = substitution.Lesson;
            existing.ClassId = substitution.ClassId;
            existing.SubjectId = substitution.SubjectId;
            existing.AbsentTeacherId = substitution.AbsentTeacherId;
            existing.SubstituteTeacherId = substitution.SubstituteTeacherId;
            existing.RoomId = substitution.RoomId;
            existing.IsCancelled = substitution.IsCancelled;
            existing.Note = substitution.Note;

            await context.SaveChangesAsync();
            _logger.LogDebug("Zaktualizowano zastępstwo {SubstitutionId}.", substitution.Id);
            return true;
        }

        public async Task<bool> RemoveSubstitutionAsync(int id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var substitution = await context.Substitutions.FirstOrDefaultAsync(s => s.Id == id);
            if (substitution == null)
            {
                _logger.LogWarning("Nie można usunąć zastępstwa {SubstitutionId}: nie istnieje.", id);
                return false;
            }

            // Remove() zamienia się na soft delete (UPDATE IsDeleted = 1, patrz NewspaperDbContext)
            context.Substitutions.Remove(substitution);
            await context.SaveChangesAsync();
            _logger.LogDebug("Oznaczono zastępstwo {SubstitutionId} jako usunięte.", id);
            return true;
        }

        public async Task<bool> LessonTakenAsync(DateTime date, int lesson, int classId, int? excludeId = null)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var day = date.Date;
            return await context.Substitutions.AnyAsync(s =>
                s.Date == day && s.Lesson == lesson && s.ClassId == classId
                && (excludeId == null || s.Id != excludeId));
        }
    }
}

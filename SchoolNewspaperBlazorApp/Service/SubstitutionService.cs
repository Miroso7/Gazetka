using Microsoft.Extensions.Logging;
using SchoolNewspaperBlazorApp.Data;
using SchoolNewspaperBlazorApp.Data.Enums;
using SchoolNewspaperBlazorApp.Interfaces.Repository;
using SchoolNewspaperBlazorApp.Interfaces.Service;

namespace SchoolNewspaperBlazorApp.Service
{
    public class SubstitutionService : ISubstitutionService
    {
        private readonly ISubstitutionRepository _substitutionRepository;
        private readonly ISchoolDataRepository _schoolDataRepository;
        private readonly IPermissionService _permissionService;
        private readonly ILogger<SubstitutionService> _logger;

        public SubstitutionService(
            ISubstitutionRepository substitutionRepository,
            ISchoolDataRepository schoolDataRepository,
            IPermissionService permissionService,
            ILogger<SubstitutionService> logger)
        {
            _substitutionRepository = substitutionRepository;
            _schoolDataRepository = schoolDataRepository;
            _permissionService = permissionService;
            _logger = logger;
        }

        public async Task<List<Substitution>> GetSubstitutionsAsync(DateTime? date)
        {
            return await _substitutionRepository.GetSubstitutionsAsync(date);
        }

        public async Task AddSubstitutionAsync(SubstitutionInput input)
        {
            await _permissionService.RequirePermissionAsync(EnumPermissions.AddSubstitution);
            var substitution = await ValidateAsync(input, null);
            substitution.CreatedAt = DateTime.Now;
            substitution.CreatedByUserId = await _permissionService.GetMyUserIdAsync();

            await _substitutionRepository.AddSubstitutionAsync(substitution);
            _logger.LogInformation("Dodano zastępstwo {SubstitutionId} na dzień {Date}.", substitution.Id, substitution.Date);
        }

        public async Task UpdateSubstitutionAsync(SubstitutionInput input)
        {
            await _permissionService.RequirePermissionAsync(EnumPermissions.EditSubstitution);
            if (input.Id <= 0)
            {
                throw new ArgumentException("Nieprawidłowe ID zastępstwa.", nameof(input));
            }
            var substitution = await ValidateAsync(input, input.Id);
            substitution.Id = input.Id;

            if (!await _substitutionRepository.UpdateSubstitutionAsync(substitution))
            {
                throw new InvalidOperationException("Nie znaleziono zastępstwa. Odśwież listę.");
            }
            _logger.LogInformation("Zaktualizowano zastępstwo {SubstitutionId}.", input.Id);
        }

        public async Task RemoveSubstitutionAsync(int id)
        {
            await _permissionService.RequirePermissionAsync(EnumPermissions.DeleteSubstitution);
            if (id <= 0) return;

            if (!await _substitutionRepository.RemoveSubstitutionAsync(id))
            {
                throw new InvalidOperationException("Nie znaleziono zastępstwa. Odśwież listę.");
            }
            _logger.LogInformation("Usunięto zastępstwo {SubstitutionId} (soft delete).", id);
        }

        // Sprawdza formularz i zwraca zastępstwo gotowe do zapisu (bez Id, autora i daty dodania).
        private async Task<Substitution> ValidateAsync(SubstitutionInput input, int? editedId)
        {
            var date = input.Date.Date;
            if (date.Year < 2000 || date.Year > 2100)
            {
                throw new InvalidOperationException("Podaj poprawny dzień zastępstwa.");
            }

            if (input.Lesson < 1 || input.Lesson > Substitution.MaxLesson)
            {
                throw new InvalidOperationException($"Numer lekcji musi być od 1 do {Substitution.MaxLesson}.");
            }

            if (input.ClassId is not { } classId)
            {
                throw new InvalidOperationException("Wybierz klasę.");
            }
            if (input.AbsentTeacherId is not { } absentId)
            {
                throw new InvalidOperationException("Wybierz nieobecnego nauczyciela.");
            }

            int? substituteId = input.IsCancelled ? null : input.SubstituteTeacherId;
            if (!input.IsCancelled)
            {
                if (substituteId is null)
                {
                    throw new InvalidOperationException("Wybierz zastępującego nauczyciela albo zaznacz, że lekcja jest odwołana.");
                }
                if (substituteId == absentId)
                {
                    throw new InvalidOperationException("Nauczyciel nie może zastępować samego siebie.");
                }
            }

            await RequireExistsAsync(SchoolDictionaryKind.Class, classId, "klasę");
            await RequireExistsAsync(SchoolDictionaryKind.Teacher, absentId, "nieobecnego nauczyciela");
            if (substituteId is { } sid) await RequireExistsAsync(SchoolDictionaryKind.Teacher, sid, "zastępującego nauczyciela");
            if (input.SubjectId is { } subjectId) await RequireExistsAsync(SchoolDictionaryKind.Subject, subjectId, "przedmiot");
            if (input.RoomId is { } roomId) await RequireExistsAsync(SchoolDictionaryKind.Room, roomId, "salę");

            var note = input.Note?.Trim();
            if (string.IsNullOrEmpty(note)) note = null;
            if (note is { Length: > Substitution.MaxNoteLength })
            {
                throw new InvalidOperationException($"Uwagi mogą mieć maksymalnie {Substitution.MaxNoteLength} znaków.");
            }

            if (await _substitutionRepository.LessonTakenAsync(date, input.Lesson, classId, editedId))
            {
                throw new InvalidOperationException("Ta klasa ma już zastępstwo na tej lekcji w wybranym dniu.");
            }

            return new Substitution
            {
                Date = date,
                Lesson = input.Lesson,
                ClassId = classId,
                SubjectId = input.SubjectId,
                AbsentTeacherId = absentId,
                SubstituteTeacherId = substituteId,
                RoomId = input.RoomId,
                IsCancelled = input.IsCancelled,
                Note = note
            };
        }

        private async Task RequireExistsAsync(SchoolDictionaryKind kind, int id, string what)
        {
            if (!await _schoolDataRepository.ExistsAsync(kind, id))
            {
                throw new InvalidOperationException($"Wybrana pozycja ({what}) już nie istnieje. Odśwież listę i wybierz ponownie.");
            }
        }
    }
}

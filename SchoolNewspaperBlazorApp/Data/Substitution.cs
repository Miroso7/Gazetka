using System.ComponentModel.DataAnnotations;

namespace SchoolNewspaperBlazorApp.Data
{
    // Jedno zastępstwo: kto kogo zastępuje, kiedy, w jakiej klasie i sali.
    // Klucze obce są opcjonalne w bazie (słowniki usuwa się miękko), a komplet wymaganych pól pilnuje SubstitutionService.
    public class Substitution : ISoftDelete
    {
        public const int MaxLesson = 12;
        public const int MaxNoteLength = 300;

        public int Id { get; set; }

        // dzień zastępstwa (bez godziny)
        public DateTime Date { get; set; }
        // numer lekcji (1–MaxLesson)
        public int Lesson { get; set; }

        public int? ClassId { get; set; }
        public SchoolClass? Class { get; set; }

        public int? SubjectId { get; set; }
        public Subject? Subject { get; set; }

        public int? AbsentTeacherId { get; set; }
        public Teacher? AbsentTeacher { get; set; }

        // brak zastępcy, gdy lekcja jest odwołana
        public int? SubstituteTeacherId { get; set; }
        public Teacher? SubstituteTeacher { get; set; }

        public int? RoomId { get; set; }
        public Room? Room { get; set; }

        public bool IsCancelled { get; set; }
        [MaxLength(MaxNoteLength)]
        public string? Note { get; set; }

        // Id użytkownika, który dodał zastępstwo
        public int CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}

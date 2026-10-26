namespace SchoolNewspaperBlazorApp.Data
{
    // Dane z formularza zastępstwa (nowego lub edytowanego). Id = 0 oznacza nowe zastępstwo.
    public class SubstitutionInput
    {
        public int Id { get; set; }
        public DateTime Date { get; set; } = DateTime.Today;
        public int Lesson { get; set; } = 1;
        public int? ClassId { get; set; }
        public int? SubjectId { get; set; }
        public int? AbsentTeacherId { get; set; }
        public int? SubstituteTeacherId { get; set; }
        public int? RoomId { get; set; }
        public bool IsCancelled { get; set; }
        public string? Note { get; set; }

        public static SubstitutionInput From(Substitution s) => new()
        {
            Id = s.Id,
            Date = s.Date,
            Lesson = s.Lesson,
            ClassId = s.ClassId,
            SubjectId = s.SubjectId,
            AbsentTeacherId = s.AbsentTeacherId,
            SubstituteTeacherId = s.SubstituteTeacherId,
            RoomId = s.RoomId,
            IsCancelled = s.IsCancelled,
            Note = s.Note
        };
    }
}

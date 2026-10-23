namespace SchoolNewspaperBlazorApp.Data
{
    public class MediaFile : ISoftDelete
    {
        public int Id { get; set; }
        public required string FileType { get; set; }
        public required string FileName { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

    }
}

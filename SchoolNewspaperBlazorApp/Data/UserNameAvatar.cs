namespace SchoolNewspaperBlazorApp.Data
{
    // Imię i nazwisko konta razem z nazwą pliku zdjęcia profilowego (null = brak zdjęcia).
    // Służy do dopasowania podpisu autora artykułu do zdjęcia profilowego.
    public record UserNameAvatar(string FirstName, string LastName, string? FileName);
}

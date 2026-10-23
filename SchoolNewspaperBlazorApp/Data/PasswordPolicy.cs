namespace SchoolNewspaperBlazorApp.Data
{
    // Wymagania wobec nowego hasła. Górny limit chroni przed kosztownym liczeniem hasha z gigantycznych danych.
    public static class PasswordPolicy
    {
        public const int MinLength = 10;
        public const int MaxLength = 128;
    }
}

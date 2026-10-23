namespace SchoolNewspaperBlazorApp.Data
{
    // Wynik logowania: czy dane były poprawne i czy konto musi najpierw ustawić własne hasło.
    public record LoginResult(bool Success, bool MustChangePassword)
    {
        public static LoginResult Failed => new(false, false);
    }
}

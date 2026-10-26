namespace SchoolNewspaperBlazorApp.Data
{
    // Wynik logowania: czy dane były poprawne, czy konto musi najpierw ustawić własne hasło i które to konto (UserId).
    public record LoginResult(bool Success, bool MustChangePassword, int UserId = 0)
    {
        public static LoginResult Failed => new(false, false);
    }
}

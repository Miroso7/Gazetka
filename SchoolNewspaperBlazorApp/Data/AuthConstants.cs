namespace SchoolNewspaperBlazorApp.Data
{
    public static class AuthConstants
    {
        // claim w cookie: konto musi zmienić hasło, zanim dostanie dostęp do panelu
        public const string MustChangePasswordClaim = "must_change_password";

        // polityka dla stron wymagających zalogowania i ustawionego własnego hasła
        public const string FullAccessPolicy = "FullAccess";
    }
}

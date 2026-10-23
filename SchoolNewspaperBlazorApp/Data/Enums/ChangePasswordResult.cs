namespace SchoolNewspaperBlazorApp.Data.Enums
{
    // Wynik zmiany hasła. Nazwa wartości trafia do adresu strony (?error=...), więc nie zmieniaj istniejących nazw.
    public enum ChangePasswordResult
    {
        Success = 0,
        UserNotFound = 1,
        WrongCurrentPassword = 2,
        PasswordsDoNotMatch = 3,
        TooShort = 4,
        TooLong = 5,
        SameAsCurrent = 6,
        SameAsEmail = 7,
    }

    public static class ChangePasswordResultExtensions
    {
        public static string Message(this ChangePasswordResult result) => result switch
        {
            ChangePasswordResult.UserNotFound => "Nie znaleziono konta. Zaloguj się ponownie.",
            ChangePasswordResult.WrongCurrentPassword => "Obecne hasło jest nieprawidłowe.",
            ChangePasswordResult.PasswordsDoNotMatch => "Nowe hasło i jego powtórzenie nie są takie same.",
            ChangePasswordResult.TooShort => $"Nowe hasło musi mieć co najmniej {PasswordPolicy.MinLength} znaków i nie może składać się z samych spacji.",
            ChangePasswordResult.TooLong => $"Nowe hasło może mieć najwyżej {PasswordPolicy.MaxLength} znaków.",
            ChangePasswordResult.SameAsCurrent => "Nowe hasło musi się różnić od obecnego.",
            ChangePasswordResult.SameAsEmail => "Hasło nie może być takie samo jak adres e-mail.",
            _ => string.Empty
        };
    }
}

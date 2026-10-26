namespace SchoolNewspaperBlazorApp.Interfaces.Service
{
    // Serwerowy rejestr sesji logowania: pozwala unieważnić sesję (wylogowanie, wygaśnięcie)
    // również w już połączonych obwodach Blazora, które trzymają stare uwierzytelnienie w pamięci.
    public interface ISessionRegistry
    {
        // userKey (niezmienne ID konta jako tekst) pozwala później unieważnić wszystkie sesje tego użytkownika naraz.
        string Create(string userKey);
        bool IsActive(string? sessionId);
        // Przedłuża aktywną sesję (odpowiednik SlidingExpiration cookie).
        void Touch(string? sessionId);
        void Revoke(string? sessionId);
        // Kończy wszystkie sesje danego konta (np. po zmianie hasła), także na innych urządzeniach.
        void RevokeAllForUser(string? userKey);
    }
}

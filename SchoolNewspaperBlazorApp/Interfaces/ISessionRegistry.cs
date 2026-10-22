namespace SchoolNewspaperBlazorApp.Interfaces.Service
{
    // Serwerowy rejestr sesji logowania: pozwala unieważnić sesję (wylogowanie, wygaśnięcie)
    // również w już połączonych obwodach Blazora, które trzymają stare uwierzytelnienie w pamięci.
    public interface ISessionRegistry
    {
        string Create();
        bool IsActive(string? sessionId);
        // Przedłuża aktywną sesję (odpowiednik SlidingExpiration cookie).
        void Touch(string? sessionId);
        void Revoke(string? sessionId);
    }
}

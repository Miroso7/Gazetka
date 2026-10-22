using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using SchoolNewspaperBlazorApp.Interfaces.Service;

namespace SchoolNewspaperBlazorApp.Service
{
    // Co minutę sprawdza, czy sesja zalogowanego użytkownika nadal istnieje.
    // Bez tego otwarty obwód Blazora trzymałby stare uwierzytelnienie mimo wylogowania lub wygaśnięcia cookie.
    public class SessionRevalidatingAuthenticationStateProvider : RevalidatingServerAuthenticationStateProvider
    {
        private readonly ISessionRegistry _sessions;

        public SessionRevalidatingAuthenticationStateProvider(ILoggerFactory loggerFactory, ISessionRegistry sessions)
            : base(loggerFactory)
        {
            _sessions = sessions;
        }

        protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(1);

        protected override Task<bool> ValidateAuthenticationStateAsync(
            AuthenticationState authenticationState, CancellationToken cancellationToken)
        {
            var user = authenticationState.User;
            if (user.Identity?.IsAuthenticated != true)
            {
                return Task.FromResult(true);
            }

            var sessionId = user.FindFirst(SessionRegistry.SessionClaimType)?.Value;
            return Task.FromResult(_sessions.IsActive(sessionId));
        }
    }
}

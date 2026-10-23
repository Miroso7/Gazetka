using Microsoft.Extensions.Logging;
using SchoolNewspaperBlazorApp.Interfaces.Service;
using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace SchoolNewspaperBlazorApp.Service
{
    // Rejestr trzymany w pamięci (singleton): po restarcie aplikacji wszyscy muszą zalogować się ponownie.
    public class SessionRegistry : ISessionRegistry
    {
        // nazwa claima z identyfikatorem sesji w cookie logowania
        public const string SessionClaimType = "sid";

        // tyle samo co ExpireTimeSpan cookie w Program.cs
        public static readonly TimeSpan Lifetime = TimeSpan.FromHours(1);

        private sealed record SessionEntry(DateTime ExpiresAt, string UserKey);

        private readonly ConcurrentDictionary<string, SessionEntry> _sessions = new();
        private readonly ILogger<SessionRegistry> _logger;

        public SessionRegistry(ILogger<SessionRegistry> logger)
        {
            _logger = logger;
        }

        public string Create(string userKey)
        {
            PurgeExpired();

            var sessionId = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            _sessions[sessionId] = new SessionEntry(DateTime.UtcNow + Lifetime, userKey);
            _logger.LogDebug("Utworzono sesję (aktywnych sesji: {Count}).", _sessions.Count);
            return sessionId;
        }

        public bool IsActive(string? sessionId)
        {
            if (string.IsNullOrEmpty(sessionId)) return false;
            if (!_sessions.TryGetValue(sessionId, out var entry)) return false;

            if (entry.ExpiresAt <= DateTime.UtcNow)
            {
                _sessions.TryRemove(sessionId, out _);
                _logger.LogDebug("Sesja wygasła i została usunięta.");
                return false;
            }
            return true;
        }

        public void Touch(string? sessionId)
        {
            if (string.IsNullOrEmpty(sessionId)) return;

            // TryUpdate nie wskrzesi sesji, którą ktoś w międzyczasie unieważnił
            if (_sessions.TryGetValue(sessionId, out var entry) && entry.ExpiresAt > DateTime.UtcNow)
            {
                _sessions.TryUpdate(sessionId, entry with { ExpiresAt = DateTime.UtcNow + Lifetime }, entry);
            }
        }

        public void Revoke(string? sessionId)
        {
            if (string.IsNullOrEmpty(sessionId)) return;

            if (_sessions.TryRemove(sessionId, out _))
            {
                _logger.LogDebug("Unieważniono sesję (aktywnych sesji: {Count}).", _sessions.Count);
            }
        }

        public void RevokeAllForUser(string? userKey)
        {
            if (string.IsNullOrWhiteSpace(userKey)) return;

            var revoked = 0;
            foreach (var session in _sessions)
            {
                if (string.Equals(session.Value.UserKey, userKey, StringComparison.OrdinalIgnoreCase)
                    && _sessions.TryRemove(session.Key, out _))
                {
                    revoked++;
                }
            }
            _logger.LogDebug("Unieważniono {Revoked} sesji użytkownika (aktywnych sesji: {Count}).", revoked, _sessions.Count);
        }

        private void PurgeExpired()
        {
            var now = DateTime.UtcNow;
            foreach (var session in _sessions)
            {
                if (session.Value.ExpiresAt <= now)
                {
                    _sessions.TryRemove(session.Key, out _);
                }
            }
        }
    }
}

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

        private readonly ConcurrentDictionary<string, DateTime> _sessions = new();

        public string Create()
        {
            PurgeExpired();

            var sessionId = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            _sessions[sessionId] = DateTime.UtcNow + Lifetime;
            return sessionId;
        }

        public bool IsActive(string? sessionId)
        {
            if (string.IsNullOrEmpty(sessionId)) return false;
            if (!_sessions.TryGetValue(sessionId, out var expiresAt)) return false;

            if (expiresAt <= DateTime.UtcNow)
            {
                _sessions.TryRemove(sessionId, out _);
                return false;
            }
            return true;
        }

        public void Touch(string? sessionId)
        {
            if (string.IsNullOrEmpty(sessionId)) return;

            // TryUpdate nie wskrzesi sesji, którą ktoś w międzyczasie unieważnił
            if (_sessions.TryGetValue(sessionId, out var expiresAt) && expiresAt > DateTime.UtcNow)
            {
                _sessions.TryUpdate(sessionId, DateTime.UtcNow + Lifetime, expiresAt);
            }
        }

        public void Revoke(string? sessionId)
        {
            if (string.IsNullOrEmpty(sessionId)) return;

            _sessions.TryRemove(sessionId, out _);
        }

        private void PurgeExpired()
        {
            var now = DateTime.UtcNow;
            foreach (var session in _sessions)
            {
                if (session.Value <= now)
                {
                    _sessions.TryRemove(session.Key, out _);
                }
            }
        }
    }
}

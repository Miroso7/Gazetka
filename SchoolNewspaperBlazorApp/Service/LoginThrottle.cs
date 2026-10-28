namespace SchoolNewspaperBlazorApp.Service
{
    // Blokada logowania (brute force): 15 minut po 5 nieudanych próbach na jeden e-mail albo po 20 próbach z jednego adresu IP.
    // Stan trzymany jest w pamięci (singleton), więc restart aplikacji zdejmuje blokady.
    public sealed class LoginThrottle
    {
        public const int MaxFailuresPerEmail = 5;
        public const int MaxFailuresPerIp = 20;
        public static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(15);

        private sealed class Entry
        {
            public int Failures;
            public DateTime WindowStart;
            public DateTime? BlockedUntil;
        }

        private readonly object _gate = new();
        private readonly Dictionary<string, Entry> _byEmail = new();
        private readonly Dictionary<string, Entry> _byIp = new();

        // Czy logowanie jest zablokowane (dla tego e-maila albo tego adresu IP); retryAfter to czas do końca blokady.
        public bool IsBlocked(string? email, string? ip, out TimeSpan retryAfter)
        {
            var now = DateTime.UtcNow;
            retryAfter = TimeSpan.Zero;

            lock (_gate)
            {
                var until = LatestBlock(_byEmail, EmailKey(email), now);
                var untilIp = LatestBlock(_byIp, ip, now);
                if (untilIp > until) until = untilIp;

                if (until is null) return false;
                retryAfter = until.Value - now;
                return true;
            }
        }

        // Zlicza nieudaną próbę (także dla nieistniejących kont, żeby nie dało się sprawdzać, jakie e-maile istnieją).
        public void RegisterFailure(string? email, string? ip)
        {
            var now = DateTime.UtcNow;
            lock (_gate)
            {
                Sweep(_byEmail, now);
                Sweep(_byIp, now);
                Count(_byEmail, EmailKey(email), MaxFailuresPerEmail, now);
                Count(_byIp, ip, MaxFailuresPerIp, now);
            }
        }

        // Udane logowanie zeruje licznik e-maila (licznik adresu IP zostaje, bo IP może być wspólny dla wielu osób).
        public void RegisterSuccess(string? email)
        {
            var key = EmailKey(email);
            if (key is null) return;
            lock (_gate)
            {
                _byEmail.Remove(key);
            }
        }

        private static string? EmailKey(string? email) =>
            string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();

        private static DateTime? LatestBlock(Dictionary<string, Entry> map, string? key, DateTime now)
        {
            if (key is null || !map.TryGetValue(key, out var entry)) return null;
            if (entry.BlockedUntil is { } until && until > now) return until;
            return null;
        }

        private static void Count(Dictionary<string, Entry> map, string? key, int limit, DateTime now)
        {
            if (key is null) return;

            if (!map.TryGetValue(key, out var entry)
                || (entry.BlockedUntil is null && now - entry.WindowStart > LockDuration)
                || (entry.BlockedUntil is { } ended && ended <= now))
            {
                // nowe okno liczenia (poprzednie wygasło albo blokada się skończyła)
                entry = new Entry { WindowStart = now };
                map[key] = entry;
            }

            if (entry.BlockedUntil is not null) return;

            entry.Failures++;
            if (entry.Failures >= limit)
            {
                entry.BlockedUntil = now + LockDuration;
            }
        }

        // usuwa wpisy, które już nic nie blokują ani nie liczą, żeby słownik nie rósł bez końca
        private static void Sweep(Dictionary<string, Entry> map, DateTime now)
        {
            if (map.Count < 1000) return;

            foreach (var key in map.Where(p => p.Value.BlockedUntil is { } until
                                                ? until <= now
                                                : now - p.Value.WindowStart > LockDuration)
                                   .Select(p => p.Key).ToList())
            {
                map.Remove(key);
            }
        }
    }
}

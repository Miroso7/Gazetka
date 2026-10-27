using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using SchoolNewspaperBlazorApp.Data;
using SchoolNewspaperBlazorApp.Data.Enums;
using SchoolNewspaperBlazorApp.Interfaces.Repository;
using SchoolNewspaperBlazorApp.Interfaces.Service;

namespace SchoolNewspaperBlazorApp.Service
{
    public class AuthService : IAuthService
    {
        // Format zapisu w bazie: PBKDF2$<iteracje>$<sól base64>$<hash base64>
        private const string HashPrefix = "PBKDF2";
        private const int SaltSize = 16;      // 128 bitów
        private const int KeySize = 32;       // 256 bitów
        private const int Iterations = 600_000;

        private readonly IAuthRepository _authRepository;
        private readonly IPermissionService _permissionService;
        private readonly IFileService _fileService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(IAuthRepository authRepository, IPermissionService permissionService, IFileService fileService, ILogger<AuthService> logger)
        {
            _authRepository = authRepository;
            _permissionService = permissionService;
            _fileService = fileService;
            _logger = logger;
        }

        public async Task AddUserAsync(string firstName, string lastName, string password, string email, UserRole role)
        {
            await _permissionService.RequirePermissionAsync(EnumPermissions.AddUser);

            if (!Enum.IsDefined(role))
            {
                throw new InvalidOperationException("Wybierz rolę użytkownika.");
            }

            if (string.IsNullOrWhiteSpace(firstName))
            {
                throw new InvalidOperationException("Imię nie może być puste.");
            }

            if (string.IsNullOrWhiteSpace(lastName))
            {
                throw new InvalidOperationException("Nazwisko nie może być puste.");
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                throw new InvalidOperationException("E-mail nie może być pusty.");
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException("Hasło nie może być puste.");
            }

            firstName = firstName.Trim();
            lastName = lastName.Trim();
            email = email.Trim();

            if (await _authRepository.EmailExistsAsync(email))
            {
                _logger.LogWarning("Nie dodano użytkownika: podany adres e-mail jest już zajęty.");
                throw new InvalidOperationException("Użytkownik o takim adresie e-mail już istnieje.");
            }

            var user = new User
            {
                FirstName = firstName,
                LastName = lastName,
                Password = HashPassword(password),
                // hasło od administratora jest tymczasowe: użytkownik ustawia własne przy pierwszym logowaniu
                MustChangePassword = true,
                Email = email,
                Role = role,
                RegisterDate = DateTime.Now
            };

            await _authRepository.AddUserAsync(user);
            _logger.LogInformation("Dodano użytkownika {UserId} (rola {Role}).", user.Id, role);
        }

        public async Task<UserProfile?> GetMyProfileAsync()
        {
            var myId = await _permissionService.GetMyUserIdAsync();
            return myId == 0 ? null : await _authRepository.GetProfileAsync(myId);
        }

        public async Task UpdateUserAsync(int userId, string firstName, string lastName, string email, UserRole role)
        {
            var myId = await _permissionService.GetMyUserIdAsync();
            if (myId == 0)
            {
                throw new UnauthorizedAccessException("Zaloguj się, aby edytować konto.");
            }

            var isMaster = await _permissionService.HasPermissionAsync(EnumPermissions.Master);
            if (!isMaster && userId != myId)
            {
                _logger.LogWarning("Użytkownik {UserId} próbował edytować konto użytkownika {TargetUserId} bez uprawnień.", myId, userId);
                throw new UnauthorizedAccessException("Możesz edytować tylko własne konto.");
            }

            var current = await _authRepository.GetProfileAsync(userId);
            if (current is null)
            {
                throw new InvalidOperationException("Nie znaleziono użytkownika.");
            }

            if (string.IsNullOrWhiteSpace(firstName))
            {
                throw new InvalidOperationException("Imię nie może być puste.");
            }

            if (string.IsNullOrWhiteSpace(lastName))
            {
                throw new InvalidOperationException("Nazwisko nie może być puste.");
            }

            firstName = firstName.Trim();
            lastName = lastName.Trim();

            // zwykły użytkownik zmienia tylko imię i nazwisko – e-mail i rola zostają bez zmian
            if (!isMaster)
            {
                email = current.Email;
                role = current.Role;
            }

            email = email?.Trim() ?? string.Empty;
            if (email.Length == 0 || !email.Contains('@'))
            {
                throw new InvalidOperationException("Podaj poprawny adres e-mail.");
            }

            if (!Enum.IsDefined(role))
            {
                throw new InvalidOperationException("Wybierz rolę użytkownika.");
            }

            var emailChanged = !string.Equals(email, current.Email, StringComparison.OrdinalIgnoreCase);
            if (emailChanged)
            {
                // e-mail zalogowanej osoby jest zapisany w jej sesji, więc jego zmiana rozłączyłaby ją z kontem
                if (userId == myId)
                {
                    throw new InvalidOperationException("Nie możesz zmienić e-maila własnego konta.");
                }

                if (await _authRepository.EmailExistsForOtherAsync(email, userId))
                {
                    throw new InvalidOperationException("Użytkownik o takim adresie e-mail już istnieje.");
                }
            }

            if (!await _authRepository.UpdateProfileAsync(userId, firstName, lastName, email, role))
            {
                throw new InvalidOperationException("Nie znaleziono użytkownika.");
            }

            _logger.LogInformation("Użytkownik {UserId} zaktualizował dane użytkownika {TargetUserId} (rola {Role}, zmiana e-maila: {EmailChanged}).",
                myId, userId, role, emailChanged);
        }

        public async Task SetMyAvatarAsync(byte[]? image)
        {
            var myId = await _permissionService.GetMyUserIdAsync();
            if (myId == 0)
            {
                throw new UnauthorizedAccessException("Zaloguj się, aby zmienić zdjęcie profilowe.");
            }

            // najpierw wgrywamy nowe zdjęcie (serwer sprawdza jego zawartość i rozmiar), dopiero potem podmieniamy je w koncie
            int? fileId = image is null ? null : await _fileService.UploadAvatarAsync(image);

            if (!await _authRepository.SetAvatarAsync(myId, fileId))
            {
                throw new InvalidOperationException("Nie znaleziono użytkownika.");
            }

            _logger.LogInformation("Użytkownik {UserId} {Action} zdjęcie profilowe.", myId, image is null ? "usunął" : "zmienił");
        }

        public async Task<Dictionary<string, string>> GetAuthorAvatarUrlsAsync(IEnumerable<string> authors)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var wanted = authors
                .Where(a => !string.IsNullOrWhiteSpace(a))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (wanted.Count == 0)
            {
                return result;
            }

            var users = await _authRepository.GetUserNamesWithAvatarsAsync();
            if (users.All(u => string.IsNullOrEmpty(u.FileName)))
            {
                return result;
            }

            // podpis -> konta o takim imieniu i nazwisku („Jan Kowalski” i „Kowalski Jan”); przy dwóch takich kontach nie zgadujemy
            var byName = new Dictionary<string, List<UserNameAvatar>>(StringComparer.OrdinalIgnoreCase);
            foreach (var user in users)
            {
                var keys = new[] { NameKey($"{user.FirstName} {user.LastName}"), NameKey($"{user.LastName} {user.FirstName}") }
                    .Where(k => k.Length > 0)
                    .Distinct(StringComparer.OrdinalIgnoreCase);
                foreach (var key in keys)
                {
                    if (!byName.TryGetValue(key, out var matches))
                    {
                        byName[key] = matches = new List<UserNameAvatar>();
                    }
                    matches.Add(user);
                }
            }

            foreach (var author in wanted)
            {
                if (byName.TryGetValue(NameKey(author), out var matches)
                    && matches.Count == 1
                    && UserProfile.AvatarUrlFor(matches[0].FileName) is { } url)
                {
                    result[author] = url;
                }
            }

            return result;
        }

        // spacje na brzegach i podwójne spacje nie mają znaczenia przy porównaniu podpisu z nazwiskiem konta
        private static string NameKey(string name) =>
            string.Join(' ', name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        public async Task<LoginResult> LoginAsync(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(password))
            {
                return LoginResult.Failed;
            }

            var user = await _authRepository.GetUserByEmailAsync(email.Trim());
            if (user is null)
            {
                _logger.LogDebug("Logowanie odrzucone: nie ma użytkownika o podanym adresie e-mail.");
                return LoginResult.Failed;
            }

            if (IsHashed(user.Password))
            {
                var verified = VerifyHash(user.Password, password);
                LogLoginResult(user.Id, verified);
                return verified ? new LoginResult(true, user.MustChangePassword, user.Id) : LoginResult.Failed;
            }

            // Stare konto z hasłem zapisanym jawnie: porównujemy w C# (rozróżnia wielkość liter),
            // a po udanym logowaniu od razu zamieniamy hasło na hash.
            if (!SecureEquals(user.Password, password))
            {
                LogLoginResult(user.Id, false);
                return LoginResult.Failed;
            }

            user.Password = HashPassword(password);
            await _authRepository.UpdateUserAsync(user);
            _logger.LogInformation("Zamieniono hasło użytkownika {UserId} zapisane jawnie na hash.", user.Id);
            LogLoginResult(user.Id, true);
            return new LoginResult(true, user.MustChangePassword, user.Id);
        }

        public async Task<ChangePasswordResult> ChangePasswordAsync(int userId, string currentPassword, string newPassword, string confirmPassword)
        {
            if (userId <= 0)
            {
                return ChangePasswordResult.UserNotFound;
            }

            // najpierw tanie sprawdzenia formularza, dopiero potem kosztowne liczenie hasha
            if (newPassword != confirmPassword)
            {
                return ChangePasswordResult.PasswordsDoNotMatch;
            }

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < PasswordPolicy.MinLength)
            {
                return ChangePasswordResult.TooShort;
            }

            if (newPassword.Length > PasswordPolicy.MaxLength)
            {
                return ChangePasswordResult.TooLong;
            }

            var user = await _authRepository.GetUserByIdAsync(userId);
            if (user is null)
            {
                _logger.LogWarning("Zmiana hasła odrzucona: nie znaleziono konta zalogowanej osoby.");
                return ChangePasswordResult.UserNotFound;
            }

            if (string.IsNullOrEmpty(currentPassword) || !PasswordMatches(user, currentPassword))
            {
                _logger.LogWarning("Zmiana hasła odrzucona: błędne obecne hasło użytkownika {UserId}.", user.Id);
                return ChangePasswordResult.WrongCurrentPassword;
            }

            if (PasswordMatches(user, newPassword))
            {
                return ChangePasswordResult.SameAsCurrent;
            }

            if (string.Equals(newPassword.Trim(), user.Email.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return ChangePasswordResult.SameAsEmail;
            }

            if (!await _authRepository.UpdatePasswordAsync(user.Id, HashPassword(newPassword)))
            {
                return ChangePasswordResult.UserNotFound;
            }

            _logger.LogInformation("Użytkownik {UserId} zmienił hasło.", user.Id);
            return ChangePasswordResult.Success;
        }

        private void LogLoginResult(int userId, bool success)
        {
            if (success)
            {
                _logger.LogInformation("Zalogowano użytkownika {UserId}.", userId);
            }
            else
            {
                _logger.LogWarning("Nieudane logowanie: błędne hasło użytkownika {UserId}.", userId);
            }
        }

        private static string HashPassword(string password)
        {
            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var hash = Rfc2898DeriveBytes.Pbkdf2(
                password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);

            return string.Join('$',
                HashPrefix,
                Iterations,
                Convert.ToBase64String(salt),
                Convert.ToBase64String(hash));
        }

        // Hasło zapisane jako hash albo (stare konta) jawnie – tak samo jak przy logowaniu.
        private static bool PasswordMatches(User user, string password) =>
            IsHashed(user.Password) ? VerifyHash(user.Password, password) : SecureEquals(user.Password, password);

        private static bool IsHashed(string? stored) =>
            stored is not null && stored.StartsWith(HashPrefix + "$", StringComparison.Ordinal);

        private static bool VerifyHash(string stored, string password)
        {
            var parts = stored.Split('$');
            if (parts.Length != 4 || !int.TryParse(parts[1], out var iterations) || iterations <= 0)
            {
                return false;
            }

            try
            {
                var salt = Convert.FromBase64String(parts[2]);
                var expected = Convert.FromBase64String(parts[3]);
                var actual = Rfc2898DeriveBytes.Pbkdf2(
                    password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);

                return CryptographicOperations.FixedTimeEquals(expected, actual);
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static bool SecureEquals(string? stored, string provided)
        {
            var a = Encoding.UTF8.GetBytes(stored ?? "");
            var b = Encoding.UTF8.GetBytes(provided);
            return CryptographicOperations.FixedTimeEquals(a, b);
        }
    }
}

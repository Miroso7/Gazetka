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

        public AuthService(IAuthRepository authRepository, IPermissionService permissionService)
        {
            _authRepository = authRepository;
            _permissionService = permissionService;
        }

        public async Task AddUserAsync(string login, string password, string email)
        {
            await _permissionService.RequirePermissionAsync(EnumPermissions.AddUser);

            login = login.Trim();
            email = email.Trim();

            if (await _authRepository.LoginExistsAsync(login))
            {
                throw new InvalidOperationException("Użytkownik o takim loginie już istnieje.");
            }

            if (await _authRepository.EmailExistsAsync(email))
            {
                throw new InvalidOperationException("Użytkownik o takim adresie e-mail już istnieje.");
            }

            var user = new User
            {
                Login = login,
                Password = HashPassword(password),
                Email = email,
                RegisterDate = DateTime.Now
            };

            await _authRepository.AddUserAsync(user);
        }

        public async Task<bool> LoginAsync(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(password))
            {
                return false;
            }

            var user = await _authRepository.GetUserByEmailAsync(email.Trim());
            if (user is null)
            {
                return false;
            }

            if (IsHashed(user.Password))
            {
                return VerifyHash(user.Password, password);
            }

            // Stare konto z hasłem zapisanym jawnie: porównujemy w C# (rozróżnia wielkość liter),
            // a po udanym logowaniu od razu zamieniamy hasło na hash.
            if (!SecureEquals(user.Password, password))
            {
                return false;
            }

            user.Password = HashPassword(password);
            await _authRepository.UpdateUserAsync(user);
            return true;
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

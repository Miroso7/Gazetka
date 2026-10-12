using SchoolNewspaperBlazorApp.Data;
using SchoolNewspaperBlazorApp.Interfaces.Service;
using Microsoft.EntityFrameworkCore;

namespace SchoolNewspaperBlazorApp.Service
{
    public class AuthService : IAuthService
    {
        private readonly NewspaperDbContext _context;

        public AuthService(NewspaperDbContext context)
        {
            _context = context;
        }

        public async Task AddUserAsync(string login, string password, string email)
        {
            var user = new User
            {
                Login = login,
                Password = password,
                Email = email,
                RegisterDate = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }
    }
}
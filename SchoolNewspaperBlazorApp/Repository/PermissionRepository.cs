using Microsoft.EntityFrameworkCore;
using SchoolNewspaperBlazorApp.Data;
using SchoolNewspaperBlazorApp.Interfaces.Repository;

namespace SchoolNewspaperBlazorApp.Repository
{
    public class PermissionRepository : IPermissionRepository
    {
        private readonly IDbContextFactory<NewspaperDbContext> _contextFactory;

        public PermissionRepository(IDbContextFactory<NewspaperDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }
    }
}

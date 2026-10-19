using SchoolNewspaperBlazorApp.Interfaces.Repository;
using SchoolNewspaperBlazorApp.Interfaces.Service;

namespace SchoolNewspaperBlazorApp.Service
{
    public class PermissionService : IPermissionService
    {
        private readonly IFileRepository _fileRepository;
        public PermissionService(IFileRepository fileRepository)
        {
            _fileRepository = fileRepository;
        }
    }
}

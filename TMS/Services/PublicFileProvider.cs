using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace TMS.Services;

// Never expose private uploads through StaticFileMiddleware, even with alternate path separators.
public sealed class PublicFileProvider(IWebHostEnvironment environment) : IFileProvider
{
    private readonly string _uploads = Path.GetFullPath(Path.Combine(environment.WebRootPath, "uploads"));
    private bool Private(string path)
    {
        try
        {
            var full = Path.GetFullPath(Path.Combine(environment.WebRootPath, path.Replace('\\', '/').TrimStart('/')));
            return full.Equals(_uploads, StringComparison.OrdinalIgnoreCase)
                || full.StartsWith(_uploads + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }
        catch (ArgumentException) { return true; }
    }
    public IFileInfo GetFileInfo(string subpath) => Private(subpath) ? new NotFoundFileInfo(subpath) : environment.WebRootFileProvider.GetFileInfo(subpath);
    public IDirectoryContents GetDirectoryContents(string subpath) => Private(subpath) ? NotFoundDirectoryContents.Singleton : environment.WebRootFileProvider.GetDirectoryContents(subpath);
    public IChangeToken Watch(string filter) => environment.WebRootFileProvider.Watch(filter);
}

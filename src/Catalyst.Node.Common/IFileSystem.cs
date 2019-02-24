using System.IO;

namespace Catalyst.Node.Common
{
    public interface IFileSystem
    {
        DirectoryInfo GetCatalystHomeDir();
    }
}
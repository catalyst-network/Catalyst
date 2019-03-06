using System.IO;

namespace Catalyst.Node.Common.Interfaces
{
    public interface IFileSystem
    {
        DirectoryInfo GetCatalystHomeDir();
    }
}
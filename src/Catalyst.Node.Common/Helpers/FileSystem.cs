using System;
using System.IO;
using Catalyst.Node.Common.Config;

namespace Catalyst.Node.Common.Helpers
{
    public class FileSystem : IFileSystem
    {
        private static string GetUserHomeDir()
        {
            var homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return homePath;
        }

        public DirectoryInfo GetCatalystHomeDir()
        {
            var path = Path.Combine(GetUserHomeDir(), Constants.CatalystSubFolder);
            return new DirectoryInfo(path);
        }
    }
}

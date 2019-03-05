using System;
using System.IO;
using Catalyst.Node.Common.Helpers.Config;
using Catalyst.Node.Common.Interfaces;

namespace Catalyst.Node.Common.Helpers.FileSystem
{
    public class FileSystem : IFileSystem
    {
        public DirectoryInfo GetCatalystHomeDir()
        {
            var path = Path.Combine(GetUserHomeDir(), Constants.CatalystSubFolder);
            return new DirectoryInfo(path);
        }

        private static string GetUserHomeDir()
        {
            var homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return homePath;
        }
    }
}
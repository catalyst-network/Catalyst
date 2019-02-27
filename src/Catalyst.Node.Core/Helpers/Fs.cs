using System;
using System.IO;
using Catalyst.Node.Core.Config;
using Catalyst.Node.Common;

namespace Catalyst.Node.Core.Helpers
{
    public class Fs : IFileSystem
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
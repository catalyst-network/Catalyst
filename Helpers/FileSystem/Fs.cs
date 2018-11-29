using System;
using System.IO;

namespace ADL.FileSystem
{
    public static class Fs
    {
        /// <summary>
        /// Gets current home directory.
        /// </summary>
        /// <param name="platform"></param>
        /// <returns></returns>
        public static DirectoryInfo GetUserHomeDir()
        {
            string dir = Environment.OSVersion.Platform == PlatformID.Unix || 
                      Environment.OSVersion.Platform == PlatformID.MacOSX
                ? Environment.GetEnvironmentVariable("HOME")
                : Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");

            if (dir == null)
            {
                throw new Exception();
            }
            
            return new DirectoryInfo(dir);
        }
    }
}

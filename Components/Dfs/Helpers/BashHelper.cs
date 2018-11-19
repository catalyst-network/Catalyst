using System.Diagnostics;
using System.Threading;

namespace ADL.DFS.Helpers
{
    /// <summary>
    ///   Helper class to run shell script commands
    /// </summary>
    /// <remarks>
    ///   Based on tutorial from <see href="https://loune.net/2017/06/running-shell-bash-commands-in-net-core/"></see>.
    /// </remarks>
    public static class BashHelper
    {
        public static void BackgroundCmd(this string cmd)
        {
            var escapedArgs = cmd.Replace("\"", "\\\"");
            
            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{escapedArgs}\"&",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            process.Start();
            Thread.Sleep(1000);
            
            // Do not wait for the process to end as it is running (allegedly) in background
            // Fire and forget
            //
        }
    }
}

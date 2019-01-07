using System;
using System.Diagnostics;
using System.Threading;

namespace ADL.Bash
{
    /// <summary>
    ///   Helper class to run shell script commands
    /// </summary>
    /// <remarks>
    ///   Based on tutorial from <see href="https://loune.net/2017/06/running-shell-bash-commands-in-net-core/"></see>.
    /// </remarks>
    public static class BashCommand
    {
        
       /// <summary>
       ///   Run a bash command in background. It can be invoked an extension method.
       /// </summary>
       /// <param name="cmd"></param>
       /// <remarks>
       ///   It assumes the operating system it has a bash shell
       /// </remarks> 
        public static void BackgroundCmd(this string cmd)
        {
            if (string.IsNullOrEmpty(cmd)) throw new ArgumentException("Value cannot be null or empty.", nameof(cmd));
            if (string.IsNullOrWhiteSpace(cmd))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(cmd));
            var escapedArgs = cmd.Replace("\"", "\\\"");
            
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{escapedArgs}\"&",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            Thread.Sleep(1000);
            
            // Do not wait for the process to end as it is running (allegedly) in background
            // Fire and forget. It is up to the caller to make sure the processes in the
            // system are killed or exited
        }
        
       /// <summary>
       ///   Run a bash command and wait for it to complete.
       ///   If the command does not responds within a configured threshold
       ///   it will throw InvalidOperationException.
       /// </summary>
       /// <param name="cmd"></param>
       /// <remarks>
       ///   It assumes the operating system it has a bash shell
       /// </remarks>
        public static string WaitForCmd(this string cmd)
        {
            if (string.IsNullOrEmpty(cmd)) throw new ArgumentException("Value cannot be null or empty.", nameof(cmd));
            if (string.IsNullOrWhiteSpace(cmd))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(cmd));
            var escapedArgs = cmd.Replace("\"", "\\\"");
            
            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{escapedArgs}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            process.Start();
            
            var result = process.StandardOutput.ReadToEnd();
            if (process.WaitForExit(10000)) return result;
            
            process.Kill();
            throw new InvalidOperationException();
        }
    }
}
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Catalyst.Helpers.Util;

namespace Catalyst.Helpers.Network
{
    public class Dns
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="hostname"></param>
        /// <returns></returns>
        private static IList<string> GetTxtRecords(string hostname)
        {
            Guard.NotNull(hostname, nameof(hostname));
            Guard.NotEmpty(hostname,nameof(hostname));
            IList<string> txtRecords = new List<string>();
            string output;
            string pattern = $@"{hostname}\s*text =\s*""([\w\-\=]*)""";

            var startInfo = new ProcessStartInfo("nslookup")
            {
                Arguments = $"-type=TXT {hostname}",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            using (var cmd = Process.Start(startInfo))
            {
                output = cmd.StandardOutput.ReadToEnd();
            }
            
            MatchCollection matches = Regex.Matches(output, pattern, RegexOptions.IgnoreCase);
            foreach (Match match in matches)
            {
                if (match.Success)
                txtRecords.Add(match.Groups[1].Value);
            }
            return txtRecords;
        }
    }
}
namespace ADL.Network
{
    public class Dns
    {
        private static IList<string> GetTxtRecords(string hostname)
        {
            IList<string> txtRecords = new List<string>();
            string output;
            string pattern = string.Format(@"{0}\s*text =\s*""([\w\-\=]*)""", hostname);
            
            var startInfo = new ProcessStartInfo("nslookup");
            startInfo.Arguments = string.Format("-type=TXT {0}", hostname);
            startInfo.RedirectStandardOutput = true;
            startInfo.UseShellExecute = false;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            
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
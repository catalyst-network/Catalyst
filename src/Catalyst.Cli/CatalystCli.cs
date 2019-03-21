using Catalyst.Node.Common.Interfaces;
using System.Diagnostics;
using Serilog;
using Serilog.Core;

namespace Catalyst.Cli
{

    public class CatalystCli : ICatalystCli
    {
        public IAds Ads { get; set; }

        public CatalystCli(IAds ads, ILogger logger)
        {
            logger.Debug("PID " + Process.GetCurrentProcess().Id);
            Ads = ads;
        }
    }
}
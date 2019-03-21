using System.Reflection;

namespace Catalyst.Node.Common.Helpers.Util
{
    public static class NodeUtil
    {
        public static string GetVersion()
        {
            return Assembly.GetEntryAssembly().GetName().Version.ToString();
        }
    }
}
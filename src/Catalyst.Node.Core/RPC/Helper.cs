namespace Catalyst.Node.Core.RPC
{
    using Microsoft.Extensions.Configuration;
    using DotNetty.Common.Internal.Logging;
    using Microsoft.Extensions.Logging;
    using System.Runtime;
    using System.IO;
    using System.Text.RegularExpressions;

    public static class Helper
    {
        static Helper()
        {
            Configuration = new ConfigurationBuilder()
                .SetBasePath(ProcessDirectory)
                .AddJsonFile("Config/rpcsettings.json")
                .Build();
        }

        public static string ProcessDirectory
        {
            get
            {
#if NETSTANDARD1_3
                return AppContext.BaseDirectory;
#else
                //return AppDomain.CurrentDomain.BaseDirectory;
                return GetApplicationRoot();
#endif
            }
        }

        public static IConfigurationRoot Configuration { get; }

        //public static void SetConsoleLogger() => InternalLoggerFactory.DefaultFactory.AddProvider(new ConsoleLoggerProvider((s, level) => true, false));

        public static string GetApplicationRoot()
        {
            var exePath =   Path.GetDirectoryName(System.Reflection
                            .Assembly.GetExecutingAssembly().Location);
            //Regex appPathMatcher=new Regex(@"(?<!file)[A-Za-z]:\\+[\S\s]*?(?=\\+bin)");
            //var appRoot = appPathMatcher.Match(exePath).Value;
            var appRoot = exePath;
            return appRoot;
        }
    }
}
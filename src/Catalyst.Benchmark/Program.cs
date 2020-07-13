using System.Linq;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;

namespace Catalyst.Benchmark
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, new AllowNonOptimized());
        }

        private class AllowNonOptimized : ManualConfig
        {
            public AllowNonOptimized()
            {
                AddValidator(JitOptimizationsValidator.DontFailOnError);
                AddLogger(DefaultConfig.Instance.GetLoggers().ToArray());
                AddExporter(DefaultConfig.Instance.GetExporters().ToArray()); 
                AddColumnProvider(DefaultConfig.Instance.GetColumnProviders().ToArray());
            }
        }
    }
}


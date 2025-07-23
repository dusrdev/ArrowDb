using BenchmarkDotNet.Running;

namespace ArrowDbCore.Benchmarks.VersionComparison;

public class Program
{
    public static void Main(string[] args)
    {
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, new VersionComparisonConfig());
    }
}
using ArrowDbCore.Benchmarks.VersionComparison;

using BenchmarkDotNet.Running;

BenchmarkRunner.Run<RandomOperationsBenchmarks>();

// public class Program
// {
//     public static void Main(string[] args)
//     {
//         BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, new VersionComparisonConfig());
//     }
// }
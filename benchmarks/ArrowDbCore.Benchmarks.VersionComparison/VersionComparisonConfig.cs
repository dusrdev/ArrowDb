using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Toolchains.CsProj;

namespace ArrowDbCore.Benchmarks.VersionComparison;

public class VersionComparisonConfig : ManualConfig
{
    public VersionComparisonConfig()
    {
        SummaryStyle = SummaryStyle.Default.WithRatioStyle(RatioStyle.Trend);

        AddJob(Job.Default
            .WithToolchain(CsProjCoreToolchain.NetCoreApp90)
            .WithId("Current"));

        AddJob(Job.Default
            .WithNuGet("ArrowDb", "1.4.0")
            .WithBaseline(true)
            .WithId("Stable"));
    }
}

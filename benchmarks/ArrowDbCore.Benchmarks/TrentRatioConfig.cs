using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;

namespace ArrowDbCore.Benchmarks;

public class TrentRatioConfig : ManualConfig {
    public TrentRatioConfig() {
        SummaryStyle = BenchmarkDotNet.Reports.SummaryStyle.Default.WithRatioStyle(RatioStyle.Trend);
    }
}
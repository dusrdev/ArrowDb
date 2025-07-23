using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;

using NuGet.Common;

using NuGet.Protocol;

using NuGet.Protocol.Core.Types;

using NuGet.Versioning;

namespace ArrowDbCore.Benchmarks.VersionComparison;

public class VersionComparisonConfig : ManualConfig {
    public const string PackageId = "ArrowDb";

    public VersionComparisonConfig() {
        var (stable, latest) = GetLatestVersionsAsync(PackageId)
            .GetAwaiter()
            .GetResult();

        SummaryStyle = SummaryStyle.Default.WithRatioStyle(RatioStyle.Trend);

        AddJob(Job.MediumRun
            .WithBaseline(true)
            .WithNuGet(PackageId, stable.ToNormalizedString())
            .WithId($"Stable-{stable.ToNormalizedString()}"));

        AddJob(Job.MediumRun
            .WithNuGet(PackageId, latest.ToNormalizedString())
            .WithId($"Latest-{latest.ToNormalizedString()}"));
    }

    private static async Task<(NuGetVersion stable, NuGetVersion latest)> GetLatestVersionsAsync(string packageId)
    {
        // Point at the official NuGet v3 API
        var source = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
        var metaResource = await source.GetResourceAsync<PackageMetadataResource>();

        // Fetch all versions (incl. prerelease) and filter out unlisted packages
        var allMetadata = await metaResource.GetMetadataAsync(
            packageId,
            includePrerelease: true,
            includeUnlisted: false,
            sourceCacheContext: new SourceCacheContext(),
            log: NullLogger.Instance,
            token: CancellationToken.None);

        // Extract distinct versions
        var versions = allMetadata
            .Select(meta => meta.Identity.Version)
            .Distinct()
            .OrderBy(v => v)         // ascending
            .ToList();

        // Highest overall version (could be prerelease)
        var latest = versions.Last();

        // Highest *stable* (no prerelease); if none, fall back to latest
        var stableVersions = versions.Where(v => !v.IsPrerelease).ToList();
        var stable = stableVersions.Any() ? stableVersions.Last() : latest;

        return (stable, latest);
    }
}

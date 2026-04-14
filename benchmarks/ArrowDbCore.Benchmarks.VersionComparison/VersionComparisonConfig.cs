using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;

using System.Xml.Linq;

using GitRepository = LibGit2Sharp.Repository;

using NuGet.Common;

using NuGet.Protocol;

using NuGet.Protocol.Core.Types;
using NuGetRepository = NuGet.Protocol.Core.Types.Repository;

using NuGet.Versioning;

namespace ArrowDbCore.Benchmarks.VersionComparison;

public class VersionComparisonConfig : ManualConfig
{
    public const string PackageId = "ArrowDb";
    private const string UseLocalArrowDbProperty = "/p:UseLocalArrowDb=true";

    public VersionComparisonConfig()
    {
        var localVersion = GetLocalPackageVersion();
        var stable = GetLatestStableVersionBelowAsync(PackageId, localVersion)
            .GetAwaiter()
            .GetResult();

        SummaryStyle = SummaryStyle.Default.WithRatioStyle(RatioStyle.Trend);
        HideColumns("Arguments");

        AddJob(Job.MediumRun
            .WithBaseline(true)
            .WithMsBuildArguments(UseLocalArrowDbProperty)
            .WithId("Local"));

        AddJob(Job.MediumRun
            .WithMsBuildArguments($"/p:ArrowDbPackageVersion={stable.ToNormalizedString()}")
            .WithId($"Stable-{stable.ToNormalizedString()}"));
    }

    private static NuGetVersion GetLocalPackageVersion()
    {
        string projectFilePath = Path.Combine(GetRepositoryRoot(), "src", "ArrowDbCore", "ArrowDbCore.csproj");
        XDocument project = XDocument.Load(projectFilePath);
        string? version = project.Root?
            .Elements("PropertyGroup")
            .Elements("Version")
            .Select(element => element.Value?.Trim())
            .FirstOrDefault(value => !string.IsNullOrEmpty(value));

        return version is null
            ? throw new InvalidOperationException($"Could not determine the local package version from '{projectFilePath}'.")
            : NuGetVersion.Parse(version);
    }

    private static string GetRepositoryRoot()
    {
        string[] startPaths =
        [
            Environment.CurrentDirectory,
            AppContext.BaseDirectory,
        ];

        foreach (string startPath in startPaths)
        {
            string? repositoryPath = GitRepository.Discover(startPath);
            if (repositoryPath is null)
            {
                continue;
            }

            using GitRepository repository = new(repositoryPath);
            string workingDirectory = repository.Info.WorkingDirectory;
            if (!string.IsNullOrEmpty(workingDirectory))
            {
                return workingDirectory;
            }
        }

        throw new InvalidOperationException($"Could not locate the git repository root starting from '{Environment.CurrentDirectory}' or '{AppContext.BaseDirectory}'.");
    }

    private static async Task<NuGetVersion> GetLatestStableVersionBelowAsync(string packageId, NuGetVersion localVersion)
    {
        var source = NuGetRepository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
        var metaResource = await source.GetResourceAsync<PackageMetadataResource>();

        var allMetadata = await metaResource.GetMetadataAsync(
            packageId,
            includePrerelease: true,
            includeUnlisted: false,
            sourceCacheContext: new SourceCacheContext(),
            log: NullLogger.Instance,
            token: CancellationToken.None);

        var stable = allMetadata
            .Select(meta => meta.Identity.Version)
            .Distinct()
            .Where(version => !version.IsPrerelease && version < localVersion)
            .Max();

        return stable ?? throw new InvalidOperationException($"No stable {packageId} package lower than local version '{localVersion}' was found on NuGet.");
    }
}

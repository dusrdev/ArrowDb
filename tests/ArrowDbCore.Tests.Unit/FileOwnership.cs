using System.Diagnostics;
using System.Security.Cryptography;

using ArrowDbCore.Serializers;
using ArrowDbCore.Tests.Probes.FileOwnership;

namespace ArrowDbCore.Tests.Unit;

public sealed class FileOwnership {
    [Fact]
    public void FileSerializer_WhenPathAlreadyOwned_ThrowsInConstructor() {
        string path = Path.GetTempFileName();
        FileSerializer? serializer = null;

        try {
            serializer = new FileSerializer(path, ArrowDbJsonContext.Default.ConcurrentDictionaryStringByteArray);

            ArrowDbOwnershipException exception = Assert.Throws<ArrowDbOwnershipException>(() =>
                new FileSerializer(path, ArrowDbJsonContext.Default.ConcurrentDictionaryStringByteArray));

            Assert.Contains(path, exception.Message, StringComparison.Ordinal);
        } finally {
            serializer?.Dispose();
            FileBackedTestHelpers.DeleteArtifacts(path);
        }
    }

    [Fact]
    public void AesFileSerializer_WhenPathAlreadyOwned_ThrowsInConstructor() {
        string path = Path.GetTempFileName();
        using Aes aes = Aes.Create();
        AesFileSerializer? serializer = null;

        try {
            serializer = new AesFileSerializer(path, aes, ArrowDbJsonContext.Default.ConcurrentDictionaryStringByteArray);

            ArrowDbOwnershipException exception = Assert.Throws<ArrowDbOwnershipException>(() =>
                new AesFileSerializer(path, aes, ArrowDbJsonContext.Default.ConcurrentDictionaryStringByteArray));

            Assert.Contains(path, exception.Message, StringComparison.Ordinal);
        } finally {
            serializer?.Dispose();
            FileBackedTestHelpers.DeleteArtifacts(path);
        }
    }

    [Fact]
    public async Task CreateFromFile_WhenOwnedByAnotherProcess_ThrowsUntilOwnerExits() {
        string path = Path.GetTempFileName();
        Process? process = null;

        try {
            process = StartOwnershipProbe(path);
            await WaitForReady(process);

            await Assert.ThrowsAsync<ArrowDbOwnershipException>(() => ArrowDb.CreateFromFile(path).AsTask());

            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync(TestContext.Current.CancellationToken);

            ArrowDb db = await ArrowDb.CreateFromFile(path);
            FileBackedTestHelpers.ReleaseOwnership(db);
        } finally {
            if (process is not null) {
                process.Dispose();
            }

            FileBackedTestHelpers.DeleteArtifacts(path);
        }
    }

    private static Process StartOwnershipProbe(string path) {
        string probeAssemblyPath = typeof(OwnershipProbeMarker).Assembly.Location;
        var startInfo = new ProcessStartInfo("dotnet") {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        startInfo.ArgumentList.Add(probeAssemblyPath);
        startInfo.ArgumentList.Add("hold");
        startInfo.ArgumentList.Add(path);
        return Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start ownership probe process.");
    }

    private static async Task WaitForReady(Process process) {
        string? line = await process.StandardOutput.ReadLineAsync(TestContext.Current.CancellationToken);
        if (string.Equals(line, "READY", StringComparison.Ordinal)) {
            return;
        }

        string error = await process.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);
        throw new InvalidOperationException($"Ownership probe did not become ready. Stdout: '{line ?? "<null>"}'. Stderr: '{error}'.");
    }
}

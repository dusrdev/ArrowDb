namespace ArrowDbCore.Tests.Unit;

internal static class FileBackedTestHelpers {
    public static void ReleaseOwnership(ArrowDb db) {
        if (db.Serializer is IDisposable disposable) {
            disposable.Dispose();
        }
    }

    public static void DeleteArtifacts(string path) {
        string? directory = Path.GetDirectoryName(path);
        string fileName = Path.GetFileName(path);

        if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory)) {
            foreach (string tempFilePath in Directory.EnumerateFiles(directory, $"{fileName}.*.tmp")) {
                File.Delete(tempFilePath);
            }
        }

        DeleteIfExists(path);
        DeleteIfExists($"{path}.lock");
    }

    private static void DeleteIfExists(string path) {
        if (File.Exists(path)) {
            File.Delete(path);
        }
    }
}

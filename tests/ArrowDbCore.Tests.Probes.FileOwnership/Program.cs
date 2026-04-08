namespace ArrowDbCore.Tests.Probes.FileOwnership;

internal static class Program {
    private static async Task<int> Main(string[] args) {
        if (args.Length != 2 || !string.Equals(args[0], "hold", StringComparison.Ordinal)) {
            Console.Error.WriteLine("Usage: hold <path>");
            return 1;
        }

        ArrowDb db = await ArrowDb.CreateFromFile(args[1]);
        Console.WriteLine("READY");
        Console.Out.Flush();

        string? _ = Console.ReadLine();
        GC.KeepAlive(db);
        return 0;
    }
}

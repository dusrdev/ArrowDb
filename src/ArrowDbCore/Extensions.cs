using System.Buffers;
using System.Security.Cryptography;
using System.Text;

namespace ArrowDbCore;

/// <summary>
/// Extension methods
/// </summary>
internal static class Extensions {
    /// <summary>
    /// Converts an input into a Hex Hash in an efficient manner
    /// </summary>
    /// <param name="input"></param>
    internal static string ToSHA256Hash(string input) {
        var inputLength = Encoding.UTF8.GetMaxByteCount(input.Length);
        using var memOwner = MemoryPool<byte>.Shared.Rent(inputLength);
        Span<byte> span = memOwner.Memory.Span;
        int written = Encoding.UTF8.GetBytes(input, span);
        Span<byte> hashBuffer = stackalloc byte[32];
        SHA256.HashData(span.Slice(0, written), hashBuffer);
        return Convert.ToHexString(hashBuffer);
    }
}
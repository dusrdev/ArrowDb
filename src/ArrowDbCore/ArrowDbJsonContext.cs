using System.Collections.Concurrent;
using System.Text.Json.Serialization;

namespace ArrowDbCore;

/// <summary>
/// The internal json serializer context of ArrowDb
/// </summary>
[JsonSourceGenerationOptions(WriteIndented = false, AllowTrailingCommas = true, NumberHandling = JsonNumberHandling.AllowReadingFromString, UseStringEnumConverter = true)]
[JsonSerializable(typeof(ConcurrentDictionary<string, byte[]>))]
public partial class ArrowDbJsonContext : JsonSerializerContext {}

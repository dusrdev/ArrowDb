using System.Text.Json.Serialization;

namespace ArrowDbCore.Benchmarks;

[JsonSourceGenerationOptions(WriteIndented = false, NumberHandling = JsonNumberHandling.AllowReadingFromString, UseStringEnumConverter = true)]
[JsonSerializable(typeof(Person))]
public partial class JContext : JsonSerializerContext {}
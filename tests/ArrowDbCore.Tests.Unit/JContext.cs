using System.Text.Json.Serialization;

namespace ArrowDbCore.Tests.Unit;

[JsonSourceGenerationOptions(WriteIndented = false, NumberHandling = JsonNumberHandling.AllowReadingFromString, UseStringEnumConverter = true)]
[JsonSerializable(typeof(int))]
public partial class JContext : JsonSerializerContext { }
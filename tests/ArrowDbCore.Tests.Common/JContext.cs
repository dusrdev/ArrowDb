using System.Text.Json.Serialization;

namespace ArrowDbCore.Tests.Common;

[JsonSourceGenerationOptions(WriteIndented = false, NumberHandling = JsonNumberHandling.AllowReadingFromString, UseStringEnumConverter = true)]
[JsonSerializable(typeof(Person))]
[JsonSerializable(typeof(int))]
public partial class JContext : JsonSerializerContext { }

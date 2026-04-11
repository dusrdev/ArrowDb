using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ArrowDbCore.Tests.Unit;

public class SerializationPendingChanges
{
    [Fact]
    public async Task SerializeAsync_WhenChangeHappensDuringSerialization_DoesNotClearPendingChanges()
    {
        var serializer = new BlockingSerializer();
        var db = await ArrowDb.CreateCustom(serializer);

        var secondUpsertCommitted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int upsertEvents = 0;
        db.OnChange += (_, args) =>
        {
            if (args.ChangeType == ArrowDbChangeType.Upsert)
            {
                if (Interlocked.Increment(ref upsertEvents) == 2)
                {
                    secondUpsertCommitted.TrySetResult();
                }
            }
        };

        // Ensure SerializeAsync doesn't early-exit: it is a no-op when PendingChanges == 0.
        Assert.True(db.Upsert("seed", new PendingChangesDuringSerializeValue { X = 0 }, PendingChangesDuringSerializeJsonContext.Default.PendingChangesDuringSerializeValue));

        var hooks = new PendingChangesDuringSerializeHooks();
        PendingChangesDuringSerializeValueConverter.Hooks.Value = hooks;

        try
        {
            Task<bool> upsertTask = Task.Run(() => db.Upsert("k", new PendingChangesDuringSerializeValue { X = 1 }, PendingChangesDuringSerializeJsonContext.Default.PendingChangesDuringSerializeValue));

            await hooks.UpsertReachedValueSerialization.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

            Task serializeTask = db.SerializeAsync();
            await serializer.SerializeStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

            hooks.AllowUpsertToProceed.Set();
            await secondUpsertCommitted.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

            serializer.AllowSerializeToFinish.TrySetResult();

            bool upserted = await upsertTask;
            await serializeTask;

            Assert.True(upserted);
            Assert.True(db.PendingChanges > 0);
        }
        finally
        {
            PendingChangesDuringSerializeValueConverter.Hooks.Value = null;
        }
    }

    private sealed class BlockingSerializer : IDbSerializer
    {
        private bool _disposed;

        public readonly TaskCompletionSource SerializeStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource AllowSerializeToFinish = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool IsDisposed => _disposed;

        public ValueTask<ConcurrentDictionary<string, byte[]>> DeserializeAsync(CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return ValueTask.FromResult(new ConcurrentDictionary<string, byte[]>());
        }

        public ValueTask SerializeAsync(ConcurrentDictionary<string, byte[]> data, CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            SerializeStarted.TrySetResult();
            return new ValueTask(AllowSerializeToFinish.Task);
        }

        public void Dispose() => _disposed = true;

        public ValueTask DisposeAsync()
        {
            _disposed = true;
            return ValueTask.CompletedTask;
        }
    }

}

// These types are intentionally top-level so System.Text.Json source generation runs correctly.

internal sealed class PendingChangesDuringSerializeHooks
{
    public readonly TaskCompletionSource UpsertReachedValueSerialization = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public readonly ManualResetEventSlim AllowUpsertToProceed = new(false);
}

[JsonConverter(typeof(PendingChangesDuringSerializeValueConverter))]
internal sealed class PendingChangesDuringSerializeValue
{
    public int X { get; set; }
}

internal sealed class PendingChangesDuringSerializeValueConverter : JsonConverter<PendingChangesDuringSerializeValue>
{
    public static readonly AsyncLocal<PendingChangesDuringSerializeHooks?> Hooks = new();

    public override PendingChangesDuringSerializeValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected StartObject.");
        }
        int x = 0;
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return new PendingChangesDuringSerializeValue { X = x };
            }
            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Expected PropertyName.");
            }
            string propertyName = reader.GetString() ?? string.Empty;
            if (!reader.Read())
            {
                throw new JsonException("Unexpected end of JSON.");
            }
            if (propertyName == "x")
            {
                x = reader.GetInt32();
            }
            else
            {
                reader.Skip();
            }
        }
        throw new JsonException("Unexpected end of JSON.");
    }

    public override void Write(Utf8JsonWriter writer, PendingChangesDuringSerializeValue value, JsonSerializerOptions options)
    {
        PendingChangesDuringSerializeHooks? hooks = Hooks.Value;
        if (hooks is not null)
        {
            hooks.UpsertReachedValueSerialization.TrySetResult();
            if (!hooks.AllowUpsertToProceed.Wait(TimeSpan.FromSeconds(5)))
            {
                throw new TimeoutException("Timed out waiting for test to allow value serialization to proceed.");
            }
        }

        writer.WriteStartObject();
        writer.WriteNumber("x", value.X);
        writer.WriteEndObject();
    }
}

[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(PendingChangesDuringSerializeValue))]
internal partial class PendingChangesDuringSerializeJsonContext : JsonSerializerContext { }

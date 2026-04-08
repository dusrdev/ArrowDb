using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ArrowDbCore.Tests.Unit;

public class RollbackRace {
    [Fact]
    public async Task Upsert_WhenRacingWithRollback_EitherPersistsOrSignalsFailure() {
        var serializer = new RollbackRaceBlockingSerializer();
        var db = await ArrowDb.CreateCustom(serializer);

        var upsertCommitted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        db.OnChange += (_, args) => {
            if (args.ChangeType == ArrowDbChangeType.Upsert) {
                upsertCommitted.TrySetResult();
            }
        };

        var hooks = new RollbackRaceHooks();
        RollbackRaceValueConverter.Hooks.Value = hooks;

        try {
            Task<bool> upsertTask = Task.Run(() => db.Upsert(
                "k",
                new RollbackRaceValue { X = 1 },
                RollbackRaceJsonContext.Default.RollbackRaceValue));

            // Ensure Upsert has already passed WaitIfSerializing() and is now blocked inside JSON serialization.
            await hooks.UpsertReachedValueSerialization.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

            serializer.BlockNextDeserialize();
            Task rollbackTask = db.RollbackAsync();

            // Ensure rollback acquired the semaphore and is blocked in DeserializeAsync().
            await serializer.RollbackDeserializeStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

            // Allow Upsert to proceed to dictionary mutation while rollback is in progress.
            hooks.AllowUpsertToProceed.Set();
            await upsertCommitted.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

            // Now allow rollback to return an empty state, causing a Source swap that can drop the just-written key.
            serializer.AllowRollbackDeserializeToReturn.TrySetResult();

            bool upserted = await upsertTask;
            await rollbackTask;

            // Desired contract for a future "write-gate" (epoch) solution:
            // If the write is not reliable due to concurrent rollback, the operation should report failure.
            // Today, this can be violated (Upsert returns true but the key is dropped by rollback).
            Assert.True(db.ContainsKey("k") || !upserted);
        } finally {
            RollbackRaceValueConverter.Hooks.Value = null;
        }
    }
}

internal sealed class RollbackRaceBlockingSerializer : IDbSerializer {
    private int _blockNextDeserialize;

    public readonly TaskCompletionSource RollbackDeserializeStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public readonly TaskCompletionSource AllowRollbackDeserializeToReturn = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public void BlockNextDeserialize() => Interlocked.Exchange(ref _blockNextDeserialize, 1);

    public ValueTask<ConcurrentDictionary<string, byte[]>> DeserializeAsync(CancellationToken cancellationToken = default) {
        if (Interlocked.Exchange(ref _blockNextDeserialize, 0) == 0) {
            return ValueTask.FromResult(new ConcurrentDictionary<string, byte[]>());
        }

        RollbackDeserializeStarted.TrySetResult();
        return new ValueTask<ConcurrentDictionary<string, byte[]>>(WaitAndReturnEmptyAsync());
    }

    private async Task<ConcurrentDictionary<string, byte[]>> WaitAndReturnEmptyAsync() {
        await AllowRollbackDeserializeToReturn.Task.ConfigureAwait(false);
        return new ConcurrentDictionary<string, byte[]>();
    }

    public ValueTask SerializeAsync(ConcurrentDictionary<string, byte[]> data, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
}

internal sealed class RollbackRaceHooks {
    public readonly TaskCompletionSource UpsertReachedValueSerialization = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public readonly ManualResetEventSlim AllowUpsertToProceed = new(false);
}

[JsonConverter(typeof(RollbackRaceValueConverter))]
internal sealed class RollbackRaceValue {
    public int X { get; set; }
}

internal sealed class RollbackRaceValueConverter : JsonConverter<RollbackRaceValue> {
    public static readonly AsyncLocal<RollbackRaceHooks?> Hooks = new();

    public override RollbackRaceValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        if (reader.TokenType != JsonTokenType.StartObject) {
            throw new JsonException("Expected StartObject.");
        }
        int x = 0;
        while (reader.Read()) {
            if (reader.TokenType == JsonTokenType.EndObject) {
                return new RollbackRaceValue { X = x };
            }
            if (reader.TokenType != JsonTokenType.PropertyName) {
                throw new JsonException("Expected PropertyName.");
            }
            string propertyName = reader.GetString() ?? string.Empty;
            if (!reader.Read()) {
                throw new JsonException("Unexpected end of JSON.");
            }
            if (propertyName == "x") {
                x = reader.GetInt32();
            } else {
                reader.Skip();
            }
        }
        throw new JsonException("Unexpected end of JSON.");
    }

    public override void Write(Utf8JsonWriter writer, RollbackRaceValue value, JsonSerializerOptions options) {
        RollbackRaceHooks? hooks = Hooks.Value;
        if (hooks is not null) {
            hooks.UpsertReachedValueSerialization.TrySetResult();
            if (!hooks.AllowUpsertToProceed.Wait(TimeSpan.FromSeconds(5))) {
                throw new TimeoutException("Timed out waiting for test to allow value serialization to proceed.");
            }
        }

        writer.WriteStartObject();
        writer.WriteNumber("x", value.X);
        writer.WriteEndObject();
    }
}

[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(RollbackRaceValue))]
internal partial class RollbackRaceJsonContext : JsonSerializerContext { }

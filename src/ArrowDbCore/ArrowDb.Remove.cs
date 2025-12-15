namespace ArrowDbCore;

public partial class ArrowDb {
	/// <summary>
	/// Tries to remove the specified key from the database
	/// </summary>
	/// <param name="key">The key to remove</param>
	/// <returns>True if the key was removed, false otherwise</returns>
	public bool TryRemove(ReadOnlySpan<char> key) {
		var observedEpoch = Volatile.Read(ref StateEpoch);
		WaitIfSerializing(); // block if the database is currently serializing
		var removed = Lookup.TryRemove(key, out byte[]? _);
		if (removed) {
			OnChangeInternal(ArrowDbChangeEventArgs.Remove); // trigger change event
		}
		return removed && Volatile.Read(ref StateEpoch) == observedEpoch;
	}

	/// <summary>
	/// Tries to clear the database
	/// </summary>
	/// <returns>True if the clear was completed without a concurrent rollback, false otherwise</returns>
	public bool TryClear() {
		if (Source.IsEmpty) {
			return true;
		}
		var observedEpoch = Volatile.Read(ref StateEpoch);
		WaitIfSerializing(); // block if the database is currently serializing
		Source.Clear();
		OnChangeInternal(ArrowDbChangeEventArgs.Clear); // trigger change event
		return Volatile.Read(ref StateEpoch) == observedEpoch;
	}

	/// <summary>
	/// Clears the database
	/// </summary>
	public void Clear() {
		_ = TryClear();
	}
}

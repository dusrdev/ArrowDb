namespace ArrowDbCore;

public partial class ArrowDb {
	/// <summary>
	/// Tries to remove the specified key from the database
	/// </summary>
	/// <param name="key">The key to remove</param>
	/// <returns>True if the key was removed, false otherwise</returns>
	public bool TryRemove(ReadOnlySpan<char> key) {
		WaitIfSerializing(); // block if the database is currently serializing
		var removed = Lookup.TryRemove(key, out byte[]? _);
		if (removed) {
			OnChangeInternal(ArrowDbChangeEventArgs.Remove); // trigger change event
		}
		return removed;
	}

	/// <summary>
	/// Clears the database
	/// </summary>
	public void Clear() {
		if (Source.IsEmpty) {
			return;
		}
		WaitIfSerializing(); // block if the database is currently serializing
		Source.Clear();
		OnChangeInternal(ArrowDbChangeEventArgs.Clear); // trigger change event
	}
}

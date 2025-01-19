namespace ArrowDbCore;

public partial class ArrowDb {
	/// <summary>
	/// Serializes the database
	/// </summary>
	/// <remarks>
	/// If there are no pending updates, this method does nothing, otherwise it serializes the database and resets the pending updates counter
	/// </remarks>
	public async Task SerializeAsync() {
		if (_pendingChanges == 0) {
			return;
		}
		try {
			await Semaphore.WaitAsync();
			await Serializer.SerializeAsync(Source);
			Interlocked.Exchange(ref _pendingChanges, 0); // reset pending changes
		} finally {
			Semaphore.Release();
		}
	}

	/// <summary>
	/// Waits for the semaphore if the database is currently serializing
	/// </summary>
	private void WaitIfSerializing() {
		if (Semaphore.CurrentCount == 0) {
			Semaphore.Wait();
			Semaphore.Release();
		}
	}

	/// <summary>
	/// Rolls the entire database to the last persisted state
	/// </summary>
	public async Task RollbackAsync() {
		try {
			await Semaphore.WaitAsync();
			var prevState = await Serializer.DeserializeAsync();
			Source.Clear();
			Interlocked.Exchange(ref Source, prevState);
			Lookup = Source.GetAlternateLookup<ReadOnlySpan<char>>();
			Interlocked.Exchange(ref _pendingChanges, 0);
		} finally {
			Semaphore.Release();
		}
	}
}

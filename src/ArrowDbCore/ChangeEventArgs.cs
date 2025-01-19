namespace ArrowDbCore;

/// <summary>
/// An argument that is passed to the <see cref="ArrowDb.OnChange"/> event
/// </summary>
public sealed class ArrowDbChangeEventArgs : EventArgs {
	/// <summary>
	/// A change event that represents an upsert
	/// </summary>
	public static readonly ArrowDbChangeEventArgs Upsert = new(ArrowDbChangeType.Upsert);
	/// <summary>
	/// A change event that represents a removal
	/// </summary>
	public static readonly ArrowDbChangeEventArgs Remove = new(ArrowDbChangeType.Remove);
	/// <summary>
	/// A change event that represents a clear
	/// </summary>
	public static readonly ArrowDbChangeEventArgs Clear = new(ArrowDbChangeType.Clear);

	/// <summary>
	/// The type of change that occurred
	/// </summary>
	public readonly ArrowDbChangeType ChangeType;

    private ArrowDbChangeEventArgs(ArrowDbChangeType changeType) {
        ChangeType = changeType;
    }
}

/// <summary>
/// The type of change that occurred in an <see cref="ArrowDb"/> instance
/// </summary>
public enum ArrowDbChangeType {
	/// <summary>
	/// An upsert occurred
	/// </summary>
	Upsert,
	/// <summary>
	/// A key was removed
	/// </summary>
	Remove,
	/// <summary>
	/// The db instance was cleared (all entries were removed)
	/// </summary>
	Clear
}
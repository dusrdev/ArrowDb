namespace ArrowDbCore;

public partial class ArrowDb {
	/// <summary>
	/// Provides an interface that unifies methods of upserting values to ArrowDb
	/// </summary>
	/// <typeparam name="TKey"></typeparam>
	private interface IDictionaryAccessor<TKey> where TKey : allows ref struct {
		/// <summary>
		/// Assigns the <paramref name="value"/> to the <paramref name="key"/> in <paramref name="instance"/>
		/// </summary>
		/// <param name="instance">The ArrowDb instance</param>
		/// <param name="key">The key to use</param>
		/// <param name="value">The value to add/update</param>
		void Upsert(ArrowDb instance, TKey key, byte[] value);
	}

	/// <summary>
	/// Implements <see cref="IDictionaryAccessor{TKey}"/> by using the source dictionary directly
	/// </summary>
	private readonly ref struct StringAccessor : IDictionaryAccessor<string> {
		/// <inheritdoc />
		public void Upsert(ArrowDb instance, string key, byte[] value) {
			instance.Source[key] = value;
		}
	}

	/// <summary>
	/// Implements <see cref="IDictionaryAccessor{TKey}"/> by using the lookup
	/// </summary>
    private readonly ref struct ReadOnlySpanAccessor : IDictionaryAccessor<ReadOnlySpan<char>> {
		/// <inheritdoc />
        public void Upsert(ArrowDb instance, ReadOnlySpan<char> key, byte[] value) {
			instance.Lookup[key] = value;
		}
    }
}

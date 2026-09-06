using System.Collections;
using System.Collections.Immutable;

namespace Lumoin.Vericula.SourceGenerators;

/// <summary>
/// An immutable array wrapper with element-wise equality, so a pipeline value that carries one
/// stays comparable by content instead of by the underlying array's reference identity, which is
/// what keeps an incremental generator's cache hot across otherwise-unrelated edits.
/// </summary>
/// <typeparam name="T">The equatable element type.</typeparam>
internal readonly record struct EquatableArray<T>: IEnumerable<T> where T: IEquatable<T>
{
    /// <summary>
    /// Wraps an immutable array for element-wise equality.
    /// </summary>
    /// <param name="items">The array to wrap.</param>
    public EquatableArray(ImmutableArray<T> items)
    {
        Items = items;
    }

    /// <summary>
    /// The wrapped array; default when the wrapper itself is default.
    /// </summary>
    private ImmutableArray<T> Items { get; }

    /// <summary>
    /// Gets the number of elements the array holds.
    /// </summary>
    public int Length => Items.IsDefault ? 0 : Items.Length;

    /// <summary>
    /// Gets the element at the given index.
    /// </summary>
    /// <param name="index">The zero-based index of the element to get.</param>
    public T this[int index] => Items[index];

    /// <summary>
    /// Determines whether this array and <paramref name="other"/> hold equal elements in the same order.
    /// </summary>
    /// <param name="other">The array to compare against.</param>
    /// <returns><see langword="true"/> if both arrays are default, or hold equal elements in the same order; otherwise, <see langword="false"/>.</returns>
    public bool Equals(EquatableArray<T> other)
    {
        if(Items.IsDefault || other.Items.IsDefault)
        {
            return Items.IsDefault == other.Items.IsDefault;
        }

        if(Items.Length != other.Items.Length)
        {
            return false;
        }

        for(int index = 0; index < Items.Length; index++)
        {
            if(!Items[index].Equals(other.Items[index]))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Combines the hash codes of every element into one hash code.
    /// </summary>
    /// <returns>A hash code consistent with <see cref="Equals(EquatableArray{T})"/>.</returns>
    public override int GetHashCode()
    {
        if(Items.IsDefault)
        {
            return 0;
        }

        var hash = new HashCode();
        foreach(T item in Items)
        {
            hash.Add(item);
        }

        return hash.ToHashCode();
    }

    /// <summary>
    /// Wraps an immutable array for element-wise equality.
    /// </summary>
    /// <param name="items">The array to wrap.</param>
    public static implicit operator EquatableArray<T>(ImmutableArray<T> items) => new(items);

    /// <summary>
    /// Gets an enumerator over the array's elements.
    /// </summary>
    /// <returns>An enumerator that yields every element in order.</returns>
    public ImmutableArray<T>.Enumerator GetEnumerator()
    {
        return (Items.IsDefault ? ImmutableArray<T>.Empty : Items).GetEnumerator();
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        return ((IEnumerable<T>)(Items.IsDefault ? ImmutableArray<T>.Empty : Items)).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable<T>)(Items.IsDefault ? ImmutableArray<T>.Empty : Items)).GetEnumerator();
    }
}

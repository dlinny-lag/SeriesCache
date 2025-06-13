
using System.Diagnostics;
using System.Numerics;
using System.Diagnostics.Contracts;

namespace SeriesCache;

[DebuggerDisplay("{Min}-{Max}")]
public class Range<TIndex>
    where TIndex : unmanaged, IBinaryInteger<TIndex>, ISignedNumber<TIndex>, IMinMaxValue<TIndex>, IConvertible

{
    public Range()
    {
    }
    public Range(TIndex min, TIndex max)
    {
        Min = min;
        Max = max;
    }

    public TIndex Min { get; protected set; }
    public TIndex Max { get; protected set; }
}

internal sealed partial class Range<TObject, TIndex> : Range<TIndex>
    where TObject : struct
    where TIndex : unmanaged, IBinaryInteger<TIndex>, ISignedNumber<TIndex>, IMinMaxValue<TIndex>, IConvertible
{
    private readonly TObject[] _objects;

    public Range(Span<TObject> segment)
    {
        if (segment.IsEmpty)
            throw new ArgumentException("Sequence must not be empty", nameof(segment));

        if (!IndexAccessor.IsRegistered<TObject, TIndex>())
            throw new InvalidOperationException("Index filed accessor is not initialized");

        _objects = new TObject[segment.Length];

        TIndex? prev = null;
        for(int i = 0; i < segment.Length; i++)
        {
            if (prev is null)
            {
                prev = IndexAccessor.GetIndex<TObject, TIndex>(ref segment[i]);
                Min = prev.Value;
                _objects[i] = segment[i];
                continue;
            }
            TIndex curr = IndexAccessor.GetIndex<TObject, TIndex>(ref segment[i]);
            if (curr <= prev.Value) 
                throw new ArgumentException("Must be ordered ascending. No duplications allowed", nameof(segment)); // TODO: apply OverwriteMode
            prev = curr;

            _objects[i] = segment[i];
        }
        Max = prev!.Value;
    }

    /// <summary>
    /// Returns new object
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    [Pure]
    public Range<TObject, TIndex> Merge(Range<TObject, TIndex> other, OverwriteMode mode = default)
    {
        if (Min <= other.Min)
            return new Range<TObject, TIndex>(mode, _objects, other._objects);
        else
            return new Range<TObject, TIndex>(mode, other._objects, _objects);
    }

    public ReadOnlyMemory<TObject> Values => _objects;
    public ReadOnlySpan<TObject> ValuesRef => _objects;

    internal TObject[] ToArray() => (TObject[])_objects.Clone();

    public TIndex Center => (Min+Max) >> 1;

    /// <summary>
    /// <para>Returns position of <paramref name="index"/> in this range, if found.</para>
    /// <para>Returns negative value when <paramref name="index"/> is out of range.</para>
    /// <para>Returns <see langword="null" /> when <paramref name="index"/> is in range but has no position</para>
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public int? PositionOf(TIndex index)
    {
        if (index.IsInRange(Min, Max) != InRange.In)
            return -1;

        unsafe
        {
            int searchResult = _objects.BinarySearchUnsafe(index);
            if (searchResult < 0)
                return null;
            return searchResult;
        }
    }

    public IntersectResult<TIndex> TestInstersection(Range<TObject, TIndex> other)
    {
        return RangeCalculationExtensions.TestIntersection(Min, Max, other.Min, other.Max);
    }
}

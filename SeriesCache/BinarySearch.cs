using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace SeriesCache;

internal static class BinarySearchExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerStepThrough]
    public static int BinarySearch<TItem, TValue, TResult>(this TItem[] values, TValue value, DistanceWithValueRef<TItem, TValue, TResult> compare)
        where TResult : IBinaryInteger<TResult>, ISignedNumber<TResult>, IConvertible
    {
        return values.AsSpan().BinarySearch(value, compare);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerStepThrough]
    public static int BinarySearch<TItem, TValue, TResult>(this TItem[] values, TValue value, DistanceWithValue<TItem, TValue, TResult> compare)
        where TResult : IBinaryInteger<TResult>, ISignedNumber<TResult>, IConvertible
    {
        return values.AsSpan().BinarySearch(value, compare);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerStepThrough]
    public static int BinarySearch<TItem, TValue, TResult>(this Span<TItem> values, TValue value, DistanceWithValue<TItem, TValue, TResult> compare)
        where TResult : IBinaryInteger<TResult>, ISignedNumber<TResult>, IConvertible
    {
        return values.BinarySearch( [DebuggerStepThrough](ref TItem item) => compare(in item, value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerStepThrough]
    public static int BinarySearch<TItem, TResult>(this TItem[] values, DistanceWithoutValueReadonly<TItem, TResult> compare)
        where TResult : IBinaryInteger<TResult>, ISignedNumber<TResult>, IConvertible
    {
        return values.AsSpan().BinarySearch(compare);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerStepThrough]
    public static int BinarySearch<TItem, TResult>(this TItem[] values, DistanceWithoutValue<TItem, TResult> compare)
        where TResult : IBinaryInteger<TResult>, ISignedNumber<TResult>, IConvertible
    {
        return values.AsSpan().BinarySearch(compare);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerStepThrough]
    public static int BinarySearch<TItem, TValue, TResult>(this Span<TItem> values, TValue value, DistanceWithValueRef<TItem, TValue, TResult> compare)
        where TResult : IBinaryInteger<TResult>, ISignedNumber<TResult>, IConvertible
    {
        return values.BinarySearch( [DebuggerStepThrough](ref TItem item) => compare(in item, in value));
    }

    public static int BinarySearch<TItem, TResult>(this Span<TItem> values, DistanceWithoutValue<TItem, TResult> compare)
        where TResult : IBinaryInteger<TResult>, ISignedNumber<TResult>, IConvertible
    { 
        // optional implementation:
        // CompareWithoutValueReadonly<TItem, TResult> compareReadonly = (ref readonly TItem item) => {var temp = item; return compare(ref temp); };
        // return values.BinarySearch(compareReadonly);

        int mid, first = 0, last = values.Length-1;
        while (first <= last)
        {
            mid = first + ((last - first) >> 1);
            TResult compareResult = compare(ref values[mid]); // TODO: reduce code duplication
            if (compareResult == TResult.Zero) 
                return mid;
            if (compareResult < TResult.Zero)
                last = mid - 1;
            else
                first = mid + 1;
        }
        return ~first;
    }

    public static int BinarySearch<TItem, TResult>(this TItem[] values, TResult reference)
        where TItem: IIndexDistance<TResult>
        where TResult : IBinaryInteger<TResult>, ISignedNumber<TResult>, IConvertible
    {
        return values.AsSpan().BinarySearch(reference);
    }

    public static int BinarySearch<TItem, TResult>(this Span<TItem> values, TResult reference)
        where TItem: IIndexDistance<TResult>
        where TResult : IBinaryInteger<TResult>, ISignedNumber<TResult>, IConvertible 
    {
        int mid, first = 0, last = values.Length - 1;
        while (first <= last)
        {
            mid = first + ((last - first) >> 1);
            TResult compareResult = values[mid].GetDistance(reference); // TODO: reduce code duplication
            if (compareResult == TResult.Zero) 
                return mid;
            if (compareResult < TResult.Zero)
                last = mid - 1;
            else
                first = mid + 1;
        }
        return ~first;
    }

    public static int BinarySearch<TItem, TResult>(this Span<TItem> values, DistanceWithoutValueReadonly<TItem, TResult> compare)
        where TResult : IBinaryInteger<TResult>, ISignedNumber<TResult>, IConvertible
    {
        int mid, first = 0, last = values.Length - 1;
        while (first <= last)
        {
            mid = first + ((last - first) >> 1);
            TResult compareResult = compare(in values[mid]); // TODO: reduce code duplication
            if (compareResult == TResult.Zero) 
                return mid;
            if (compareResult < TResult.Zero)
                last = mid - 1;
            else
                first = mid + 1;
        }
        return ~first;
    }
}

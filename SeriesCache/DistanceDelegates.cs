using System.Numerics;

namespace SeriesCache;

/// <summary>
/// <para> Returns positive number if <paramref name="item"/> is greater than <paramref name="value"/> </para>
/// <para> Returns negative number if <paramref name="item"/> is lower than <paramref name="value"/> </para>
/// <para> Returns <see langword="0"/> if <paramref name="item"/> is equal to <paramref name="value"/> </para>
/// </summary>
/// <typeparam name="TItem"></typeparam>
/// <typeparam name="TValue"></typeparam>
/// <typeparam name="TResult"></typeparam>
/// <param name="item"></param>
/// <param name="value"></param>
/// <returns></returns>
public delegate TResult DistanceWithValueRef<TItem, TValue, TResult>(ref readonly TItem item, ref readonly TValue value) 
    where TResult : IBinaryInteger<TResult>, ISignedNumber<TResult>;

public delegate TResult DistanceWithValue<TItem, TValue, TResult>(ref readonly TItem item, TValue value) 
    where TResult : IBinaryInteger<TResult>, ISignedNumber<TResult>;

/// <summary>
/// Comparing the <paramref name="item"/> with some internal state.
/// </summary>
/// <typeparam name="TItem"></typeparam>
/// <typeparam name="TResult"></typeparam>
/// <param name="item"></param>
/// <returns></returns>
public delegate TResult DistanceWithoutValue<TItem, TResult>(ref TItem item) 
    where TResult : IBinaryInteger<TResult>, ISignedNumber<TResult>;

/// <summary>
/// Comparing the <paramref name="item"/> with some internal state.
/// </summary>
/// <typeparam name="TItem"></typeparam>
/// <typeparam name="TResult"></typeparam>
/// <param name="item"></param>
/// <returns></returns>
public delegate TResult DistanceWithoutValueReadonly<TItem, TResult>(ref readonly TItem item) 
    where TResult : IBinaryInteger<TResult>, ISignedNumber<TResult>;


public interface IIndexDistance<TResult>
    where TResult : IBinaryInteger<TResult>, ISignedNumber<TResult>
{
    TResult GetDistance(TResult reference);
}


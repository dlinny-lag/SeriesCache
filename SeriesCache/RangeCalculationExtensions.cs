using System.Numerics;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace SeriesCache;

[Flags]
public enum InRange
{
    None = 0,
    In = 1,
    BelowLower = 2,
    AboveHigher = 4,
}

public enum IntersectType
{
    /// <summary>
    /// invalid value
    /// </summary>
    None = 0,
    /// <summary>
    /// range <see langword="a"/> fully below range <see langword="b"/>, they do not intersect
    /// </summary>
    Below = 1,
    /// <summary>
    /// range <see langword="a"/> fully above range <see langword="b"/>, they do not intersect
    /// </summary>
    Above = 2,
    /// <summary>
    /// range <see langword="a"/> started below range <see langword="b"/> and finished inside range <see langword="b"/>
    /// </summary>
    IntersectLow = 3,
    /// <summary>
    /// range <see langword="a"/> started inside range <see langword="b"/> and finished above range <see langword="b"/>
    /// </summary>
    IntersectHigh = 4,
    /// <summary>
    /// range <see langword="a"/> is fully inside of range <see langword="b"/>
    /// </summary>
    Inside = 5,
    /// <summary>
    /// range <see langword="b"/> fully inside of range <see langword="a"/>
    /// </summary>
    Outside = 6,
}

[DebuggerDisplay("{Type}, {Distance}")]
public class IntersectResult<T>
    where T : struct, INumber<T>
{
    private IntersectResult(){}

    public IntersectResult(IntersectType type, T distance = default)
    {
        Type = type;
        Debug.Assert(type > IntersectType.None);
        Distance = distance;
        Debug.Assert(distance >= T.Zero);
    }

    public readonly IntersectType Type;
    public readonly T Distance;
    public bool IsIntersection(T distance = default) => Type >= IntersectType.IntersectLow || distance >= Distance;
}


public static class RangeCalculationExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static InRange IsInRange<T>(this T value, T min, T max)
        where T : struct, INumber<T>
    {
        Debug.Assert(min <= max);
        if (value <  min)
            return InRange.BelowLower;
        if (value > max)
            return InRange.AboveHigher;
        return InRange.In;
    }

    public static IntersectResult<T> AsResult<T>(this IntersectType type, T distance = default)
        where T : struct, INumber<T>
    { 
        return new IntersectResult<T>(type, distance);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IntersectResult<T> TestIntersection<T>(T min1, T max1, T min2, T max2)
        where T : struct, INumber<T>
    {
        Debug.Assert(min1 <= max1);
        Debug.Assert(min2 <= max2);

        if (min1 < min2)
        {
            if (max1 < min2)
                return IntersectType.Below.AsResult<T>(min2-max1);
            if (max1 < max2)
                return IntersectType.IntersectLow.AsResult<T>();
            return IntersectType.Outside.AsResult<T>();
        }

        // min1 >= min2
        if (min1 > max2)
            return IntersectType.Above.AsResult<T>(min1-max2);

        if (max1 > max2)
            return IntersectType.IntersectHigh.AsResult<T>();

        return IntersectType.Inside.AsResult<T>();
    }
}

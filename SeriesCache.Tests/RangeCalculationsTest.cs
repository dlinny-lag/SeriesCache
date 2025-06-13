
using FluentAssertions;

namespace SeriesCache.Tests;

public class RangeCalculationsTest
{
    [Test]
    public void InvertedMinMaxCausesExceptionInDebugOnly()
    {
        const bool debug = 
#if DEBUG
            true
#else
            false
#endif
            ;

        int number = 0;
        int min = 10;
        int max = -10;
        Action func = () => number.IsInRange(min, max);
#pragma warning disable 0162
        if (debug)
            func.Should().Throw<Exception>();
        else
            func.Should().NotThrow<Exception>();
#pragma warning restore 0162
    }

    public readonly struct IntersectionTestInfo
    {
        public IntersectionTestInfo(int min1, int max1, int min2, int max2, IntersectType result, int distance)
        {
            Min1 = min1;
            Max1 = max1;
            Min2 = min2;
            Max2 = max2;
            Result = result;
            Distance = distance;
        }
        public readonly int Min1;
        public readonly int Min2;
        public readonly int Max1;
        public readonly int Max2;
        public readonly IntersectType Result;
        public readonly int Distance;

        public override string ToString()
        {
            return $"{Min1}-{Max1} {Result} {Min2}-{Max2}";
        }
    }

    static readonly IntersectionTestInfo[] IntersectionTestDataset = 
    [
        new (0, 0, 0, 0, IntersectType.Inside, 0), // questionable
        new (0, 0, 1, 1, IntersectType.Below, 1),
        new (1, 1, 0, 0, IntersectType.Above, 1),

        new (1, 2, 3, 4, IntersectType.Below, 1),
        new (3, 4, 1, 2, IntersectType.Above, 1),
        new (1, 2, 2, 3, IntersectType.IntersectLow, 0),
        new (1, 3, 2, 4, IntersectType.IntersectLow, 0),

        new (11, 15, 10, 12, IntersectType.IntersectHigh, 0),
        new (10, 15, 10, 12, IntersectType.IntersectHigh, 0),

        new (2, 9, 1, 10, IntersectType.Inside, 0),
        new (1, 9, 1, 10, IntersectType.Inside, 0),
        new (2, 10, 1, 10, IntersectType.Inside, 0),
        
        new (1, 10, 2, 9, IntersectType.Outside, 0),
        new (1, 10, 2, 10, IntersectType.Outside, 0),
    ];

    [TestCaseSource(nameof(IntersectionTestDataset))]
    public void IntersectionTestShouldBeCorrect(IntersectionTestInfo testData)
    {
        var result = RangeCalculationExtensions.TestIntersection(testData.Min1, testData.Max1, testData.Min2, testData.Max2);
        result.Type.Should().Be(testData.Result, $"{testData}");
        result.Distance.Should().Be(testData.Distance);
    }
}

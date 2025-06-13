using FluentAssertions;
using System.Buffers;

namespace SeriesCache.Tests;

public class CachingDataAccessorTests
{
    struct MyObject
    {
        public int Index;
        public string Payload;

        public override string ToString()
        {
            return Payload;
        }
    }

    static CachingDataAccessorTests()
    {
        int offset = IndexAccessor.FindOffset<MyObject, int>(nameof(MyObject.Index));
        IndexAccessor.Register<MyObject, int>(offset);
    }

    static Task<IEnumerable<MyObject>> FakeDataReader(int start, int end)
    {
        List<MyObject> retVal = new List<MyObject>(end-start+1);
        for (int i = start; i <= end; i++)
        {
            retVal.Add(new MyObject(){Index = i, Payload = i.ToString()});
        }
        return Task.FromResult((IEnumerable<MyObject>)retVal);
    }

    public readonly struct TestRange
    {
        public TestRange(int start, int end)
        {
            Start = start;
            End = end;
        }
        public readonly int Start;
        public readonly int End;

        public override string ToString()
        {
            return $"{Start}-{End}";
        }
    }


    static readonly TestRange[] InterleavingRanges = 
    [
        new ( 0, 10),
        new (20, 30),
        new (40, 50),
    ];
    
    static readonly TestRange[] ReadingRanges =
    [
        new (0, 50),
        new (1, 50),
        new (0, 49),
        new (1, 49),
        new (5, 45),
        new (15, 35),
        new (15, 25),
        new (41, 50),
        new (1, 10),
        new (0, 25),
        new (1, 25),
        new (25, 50),
        new (25, 49),
        new (0, 0),
        new (1, 1),
        new (49, 49),
        new (50, 50),
    ];

    [TestCaseSource(nameof(ReadingRanges))]
    public async Task InterleavingRead(TestRange test)
    {
        var cache = new CachingDataAccessor<MyObject, int> (FakeDataReader);
        foreach (TestRange range in InterleavingRanges) 
        {
            _ = await cache.GetRange(range.Start, range.End);
        }

        var result = await cache.GetRange(test.Start, test.End);

        var toTest = result.ToArray().Select(o => o.Index).ToHashSet();
        toTest.Count.Should().BeGreaterThan(0);
        for(int i = test.Start; i <= test.End; i++)
            toTest.Contains(i).Should().BeTrue($"'{test} should contain {i}'");
    }

}

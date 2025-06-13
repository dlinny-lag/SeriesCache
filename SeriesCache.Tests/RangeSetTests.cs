using FluentAssertions;
using System.Buffers;
using System.Diagnostics;

namespace SeriesCache.Tests;

public class RangeSetTests
{
    static RangeSetTests()
    {
        int offset = IndexAccessor.FindOffset<MyEntry, int>(nameof(MyEntry.Id));
        IndexAccessor.Register<MyEntry, int>(offset);
    }

    [DebuggerDisplay("{Id}={Payload}")]
    readonly struct MyEntry
    {
        public MyEntry(int id, string payload) 
        {
            Id = id;
            Payload = payload;
        }
        public readonly int Id;
        public readonly string Payload;
    }

    [Test]
    public void TwoSimpleRanges()
    {
        var set = new RangesSet<MyEntry, int>();

        var range1 = new MyEntry[] 
        {
            new MyEntry(1, "one"),
            new MyEntry(2, "two"),
            new MyEntry(3, "three")
        };

        var range2 = new MyEntry[]
        {
            new MyEntry(10, "ten"),
            new MyEntry(11, "eleven"),
            new MyEntry(12, "twelve")
        };

        set.AddRange(range1);
        set.AddRange(range2);

        // case 1
        var sequence = set.GetRange(0, 15);

        MyEntry[] toTest = sequence.ToArray();
        
        toTest.Length.Should().Be(range1.Length + range2.Length);
        toTest.Should().Contain(range1[0]);
        toTest.Should().Contain(range1[1]);
        toTest.Should().Contain(range1[2]);
        
        toTest.Should().Contain(range2[0]);
        toTest.Should().Contain(range2[1]);
        toTest.Should().Contain(range2[2]);

        // case 2
        sequence = set.GetRange(0, 10);
        toTest = sequence.ToArray();
        toTest.Length.Should().Be(range1.Length + 1);

        toTest.Should().Contain(range1[0]);
        toTest.Should().Contain(range1[1]);
        toTest.Should().Contain(range1[2]);
        toTest.Should().Contain(range2[0]);

        toTest.Should().NotContain(range2[1]);
        toTest.Should().NotContain(range2[2]);

        // case 3
        sequence = set.GetRange(2, 10);
        toTest = sequence.ToArray();
        toTest.Length.Should().Be(3);

        toTest.Should().NotContain(range1[0]);
        toTest.Should().Contain(range1[1]);
        toTest.Should().Contain(range1[2]);
        toTest.Should().Contain(range2[0]);
        toTest.Should().NotContain(range2[1]);
        toTest.Should().NotContain(range2[2]);

        // case 4
        sequence = set.GetRange(7, 15);
        toTest = sequence.ToArray();
        toTest.Length.Should().Be(3);

        toTest.Should().NotContain(range1[0]);
        toTest.Should().NotContain(range1[1]);
        toTest.Should().NotContain(range1[2]);
        toTest.Should().Contain(range2[0]);
        toTest.Should().Contain(range2[1]);
        toTest.Should().Contain(range2[2]);
    }

    [Test]
    public void IntersectingRangesShouldBeMerged1()
    {
         var set = new RangesSet<MyEntry, int>();

        var range1 = new MyEntry[] 
        {
            new MyEntry(1, "one"),
            new MyEntry(2, "two"),
            new MyEntry(10, "ten"),
        };

        var range2 = new MyEntry[]
        {
            new MyEntry(3, "three"),
            new MyEntry(11, "eleven"),
            new MyEntry(12, "twelve"),
        };

        set.AddRange(range1);
        set.AddRange(range2);
        
        var sequence = set.GetRange(0, 15);
        sequence.SegmentsCount().Should().Be(1);

        var toTest = sequence.ToArray();
        toTest.Length.Should().Be(range1.Length + range2.Length);
        toTest.Should().Contain(range1[0]);
        toTest.Should().Contain(range1[1]);
        toTest.Should().Contain(range1[2]);
        
        toTest.Should().Contain(range2[0]);
        toTest.Should().Contain(range2[1]);
        toTest.Should().Contain(range2[2]);
    }


    [Test]
    public void IntersectingRangesShouldBeMerged2()
    {
         var set = new RangesSet<MyEntry, int>(new ReadWriteSettings{OnOverwrite = OverwriteMode.Replace});

        var range1 = new MyEntry[] 
        {
            new MyEntry(1, "one"),
            new MyEntry(2, "two"),
            new MyEntry(3, "three"),
        };

        var range2 = new MyEntry[]
        {
            new MyEntry(10, "ten"),
            new MyEntry(11, "eleven"),
            new MyEntry(12, "twelve"),
        };
        
        var range3 = new MyEntry[] // touches both
        {
            new MyEntry(3, "three"), // dup, causes intersection
            new MyEntry(6, "six"),
            new MyEntry(10, "ten"), // dup, causes intersection
        };

        set.AddRange(range1);
        set.AddRange(range2);
        set.AddRange(range3);
        
        var sequence = set.GetRange(0, 15);
        sequence.SegmentsCount().Should().Be(1);

        var toTest = sequence.ToArray();
        toTest.Length.Should().Be(range1.Length + range2.Length + range3.Length - 2);
        toTest.Should().Contain(range1[0]);
        toTest.Should().Contain(range1[1]);
        toTest.Should().Contain(range1[2]);
        
        toTest.Should().Contain(range2[0]);
        toTest.Should().Contain(range2[1]);
        toTest.Should().Contain(range2[2]);

        toTest.Should().Contain(range3[0]);
        toTest.Should().Contain(range3[1]);
        toTest.Should().Contain(range3[2]);
    }

    [Test]
    public void NonIntersectingRangesShouldNotBeMerged()
    {
        var range1 = new MyEntry[] 
        {
            new MyEntry(1, "one"),
            new MyEntry(2, "two"),
            new MyEntry(3, "three")
        };

        var range2 = new MyEntry[]
        {
            new MyEntry(10, "ten"),
            new MyEntry(11, "eleven"),
            new MyEntry(12, "twelve")
        };

        var set = new RangesSet<MyEntry, int>();
        set.AddRange(range1);
        set.AddRange(range2);

        var sequence = set.GetRange(0, 15);
        sequence.SegmentsCount().Should().Be(2);
    }


    [Test]
    public void NearRangesShouldBeMerged()
    {
        var set = new RangesSet<MyEntry, int>();
        const int maxDistance = 1;

        var range1 = new MyEntry[] 
        {
            new MyEntry(1, "one"),
            new MyEntry(2, "two"),
            new MyEntry(3, "three"),
        };

        var range2 = new MyEntry[]
        {
            new MyEntry(4, "four"),
            new MyEntry(5, "five"),
            new MyEntry(6, "six"),
        };

        var range3 = new MyEntry[]
        {
            new MyEntry(8, "eight"),
            new MyEntry(9, "nine"),
            new MyEntry(10, "ten"),
        };

        set.AddRange(range1, maxDistance);
        set.AddRange(range2, maxDistance);
        // range1 and range2 has distance 1

        var sequence = set.GetRange(0, 10);
        sequence.SegmentsCount().Should().Be(1);

        var toTest = sequence.ToArray();
        toTest.Length.Should().Be(range1.Length + range2.Length);
        toTest.Should().Contain(range1[0]);
        toTest.Should().Contain(range1[1]);
        toTest.Should().Contain(range1[2]);
        toTest.Should().Contain(range2[0]);
        toTest.Should().Contain(range2[1]);
        toTest.Should().Contain(range2[2]);

        set.AddRange(range3, maxDistance);
        // range2 and range3 has distance 2

        sequence = set.GetRange(0, 10);
        sequence.SegmentsCount().Should().Be(2); // one more segment
        toTest = sequence.ToArray();
        toTest.Length.Should().Be(range1.Length + range2.Length + range2.Length);
    }
}


internal static class ReadOnlySequenceExtension
{
    public static int SegmentsCount<T>(this ReadOnlySequence<T> sequence)
    {
        if (sequence.IsEmpty)
            return 0;
        // NOTE:
        // ReadOnlySequence can be constructed by multiple ways and Start object may be the one of following types:
        // T[]
        // MemoryManager<T>
        // ReadOnlyMemory<T>
        // ReadOnlySequenceSegment<T>
        // The only cases where Start IS NOT a ReadOnlySequenceSegment<T> - Start and End are pointing to the same object
        if (sequence.IsSingleSegment)
            return 1;
        var current = sequence.Start.GetObject() as ReadOnlySequenceSegment<T>;
        int retVal = 0;
        while (current != null)
        {
            ++retVal;
            current = current.Next;
        }
        return retVal;
    }
}
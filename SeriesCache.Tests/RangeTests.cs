

using FluentAssertions;
using System.Diagnostics;

namespace SeriesCache.Tests;

public class RangeTests
{
    static RangeTests()
    {
        int offset = IndexAccessor.FindOffset<MyEntry, int>(nameof(MyEntry.Id));
        IndexAccessor.Register<MyEntry, int>(offset);
    }


    [DebuggerDisplay("{Id}={Payload}")]
    readonly struct MyEntry : IComparable<MyEntry>
    {
        public MyEntry(int id, string payload) 
        {
            Id = id;
            Payload = payload;
        }
        public readonly int Id;
        public readonly string Payload;

        public int CompareTo(MyEntry other)
        {
            return Id - other.Id;
        }
    }

    [Test]
    public void MergingShouldProduceCorrectOrder1()
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

        var original = range1.Concat(range2).ToArray();

        Range<MyEntry, int> rangeOne = new Range<MyEntry, int>(range1);
        Range<MyEntry, int> rangeTwo = new Range<MyEntry, int>(range2);

        var directOrder = rangeOne.Merge(rangeTwo).ToArray();
        var reverseOrder = rangeTwo.Merge(rangeOne).ToArray();

        directOrder.Should().BeEquivalentTo(original, o => o.WithStrictOrdering() );
        reverseOrder.Should().BeEquivalentTo(original, o => o.WithStrictOrdering() );
    }

        [Test]
    public void MergingShouldProduceCorrectOrder2()
    {
        var range1 = new MyEntry[] 
        {
            new MyEntry(2, "two"),
            new MyEntry(3, "three"),
            new MyEntry(12, "twelve"),
        };

        var range2 = new MyEntry[]
        {
            new MyEntry(1, "one"),
            new MyEntry(10, "ten"),
            new MyEntry(11, "eleven"),
        };

        var original = range1.Concat(range2).ToList();
        original.Sort();

        Range<MyEntry, int> rangeOne = new Range<MyEntry, int>(range1.AsSpan());
        Range<MyEntry, int> rangeTwo = new Range<MyEntry, int>(range2.AsSpan());


        var directOrder = rangeOne.Merge(rangeTwo).ToArray();
        var reverseOrder = rangeTwo.Merge(rangeOne).ToArray();

        directOrder.Should().BeEquivalentTo(original, o => o.WithStrictOrdering() );
        reverseOrder.Should().BeEquivalentTo(original, o => o.WithStrictOrdering() );
    }

    [Test]
    public void IncorrectElementsOrderShouldCauseError()
    {
        var range1 = new MyEntry[] 
        {
            new MyEntry(2, "two"),
            new MyEntry(1, "one"), // wrong order
            new MyEntry(3, "three"),
        };
        Action toExcecute = () => new Range<MyEntry, int>(range1.AsSpan());
        toExcecute.Should().Throw<ArgumentException>();

        var range2 = new MyEntry[]
        {
            new MyEntry(10, "ten"),
            new MyEntry(11, "eleven"),
            new MyEntry(12, "twelve"),
        };

        toExcecute = () => new Range<MyEntry, int>(OverwriteMode.Error, range1, range2);
        toExcecute.Should().Throw<ArgumentException>();

        toExcecute = () => new Range<MyEntry, int>(OverwriteMode.Error, range2, range1);
        toExcecute.Should().Throw<ArgumentException>();
    }
}


using FluentAssertions;
using System;
using System.Runtime.InteropServices;

namespace SeriesCache.Tests;

public class IndexDescriptorTests
{
    struct MyStruct
    {
        public int Index; // index field
        public string Payload;
    }

    struct NonZeroOffsetStruct
    {
        public uint Payload;
        public DateOnly Date; // index field
    }

    struct UnregisteredStruct
    {
        public int Index;
        public string Payload; 
    }

    [Test]
    public void IndexExtractedCorrectlyWhenOffsetIsNonZero()
    {
        int foundOffset = IndexAccessor.FindOffset<NonZeroOffsetStruct, int>("Date._dayNumber");
        foundOffset.Should().Be(Marshal.SizeOf<int>());

        IndexAccessor.Register<NonZeroOffsetStruct, int>(foundOffset);

        nint payloadOffset = Marshal.OffsetOf<NonZeroOffsetStruct>(nameof(NonZeroOffsetStruct.Payload));

        NonZeroOffsetStruct one = new NonZeroOffsetStruct{Date=DateOnly.FromDayNumber(0x1111), Payload = 0xDEADF00D};
        int index = IndexAccessor.GetIndex<NonZeroOffsetStruct, int>(ref one);
        index.Should().Be(0x1111);

        NonZeroOffsetStruct[] asArray = { one };
        index = IndexAccessor.GetIndex<NonZeroOffsetStruct, int>(ref asArray[0]);
        index.Should().Be(0x1111);
    }

    public void SrtructMustBeRegistered()
    {
        var toExecute = () =>
        {
            UnregisteredStruct one = new UnregisteredStruct {Index = 1, Payload = "one"};
            var index1 = IndexAccessor.GetIndex<UnregisteredStruct, int>(ref one);
        };
        toExecute.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void IndexExtractedCorrectlyWhenOffsetIsZero()
    {
        nint payloadOffset = Marshal.OffsetOf<MyStruct>(nameof(MyStruct.Payload));

        int offset = IndexAccessor.FindOffset<MyStruct, int>("Index");
        IndexAccessor.Register<MyStruct, int>(offset);


        MyStruct one = new MyStruct { Payload = "one", Index = 0x1111 };
        MyStruct two = new MyStruct { Payload = "two", Index = 0x2222 };

        MyStruct[] asArray = { one, two };

        int index1 = IndexAccessor.GetIndex<MyStruct, int>(ref one);
        index1.Should().Be(0x1111);

        var index2 = IndexAccessor.GetIndex<MyStruct, int>(ref two);
        index2.Should().Be(0x2222);

        index1 = IndexAccessor.GetIndex<MyStruct, int>(ref asArray[0]);
        index1.Should().Be(0x1111);
        
        index2 = IndexAccessor.GetIndex<MyStruct, int>(ref asArray[1]);
        index2.Should().Be(0x2222);

    }



    struct OverwriteSample
    {
        public int Index; // index field
        public string Payload;
    }

    [Test]
    public void AttemptToChangeOffsetSizeShouldCauseError()
    {
        int offset = IndexAccessor.FindOffset<OverwriteSample, int>(nameof(OverwriteSample.Index));
        IndexAccessor.Register<OverwriteSample, int>(offset); // first time, ok
        IndexAccessor.Register<OverwriteSample, int>(offset); // second time, ok
        Action fail = () => {IndexAccessor.Register<OverwriteSample, int>(offset+1); };
        fail.Should().Throw<InvalidOperationException>();
    }

}

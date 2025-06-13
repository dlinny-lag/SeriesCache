
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using SeriesCache;

var summary = BenchmarkRunner.Run<BinarySearchBenchmark>();

struct MyStruct : IIndexDistance<int>
{
    public int Index;
    public string Payload;

    public int GetDistance(int reference)
    {
        return reference - Index;
    }
}

public class BinarySearchBenchmark
{
    const int count = 10000;
    private static readonly MyStruct[] data;
    static BinarySearchBenchmark()
    {
        data = new MyStruct[count];
        for(int i = 0; i < count; i++) 
        {
            data[i].Index = i;
            data[i].Payload = i.ToString();
        }
        int offset = IndexAccessor.FindOffset<MyStruct, int>(nameof(MyStruct.Index));
        IndexAccessor.Register<MyStruct, int>(offset);
    }

    const int toSearch = 0;

    int DistanceDelegate(ref readonly MyStruct value, int reference)
    {
        return reference - value.Index;
    }

    //[Benchmark] // outsider
    //public void GetIndex()
    //{
    //    int index = data.BinarySearch((ref MyStruct item) => toSearch - IndexAccessor.GetIndex<MyStruct, int>(ref item));
    //}


    [Benchmark]
    public void Interface()
    {
        int index = data.BinarySearch(toSearch);
    }

    [Benchmark]
    public void Direct()
    {
        int index = data.BinarySearch((ref MyStruct item) => toSearch - item.Index);
    }

    //[Benchmark] // outsider
    //public void Delegate()
    //{
    //    int index = data.BinarySearch(toSearch, DistanceDelegate);
    //}

    [Benchmark]
    public void UnmanagedByValue()
    {
        unsafe
        {
            int index = data.BinarySearchUnsafe(toSearch);
        }
    }
}
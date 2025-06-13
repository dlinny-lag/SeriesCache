using System.Buffers;

namespace SeriesCache;

public static class MemorySequenceHelper
{
    public static IEnumerable<T> AsEnumerable<T>(this ReadOnlySequence<T> sequence)
    {
        foreach (ReadOnlyMemory<T> item in sequence) 
        {
            for (int i = 0; i < item.Span.Length; i++)
                yield return item.Span[i];
        }
    }
}

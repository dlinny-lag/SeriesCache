using System.Buffers;
using System.Collections;
using System.Numerics;
using System.Text;

namespace SeriesCache;

public class CachingDataAccessor<TObject, TIndex>
    where TObject : struct
    where TIndex : unmanaged, IBinaryInteger<TIndex>, ISignedNumber<TIndex>, IMinMaxValue<TIndex>, IConvertible
{
    /// <summary>
    /// must returns objects sequence ordered ascending
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    public delegate Task<IEnumerable<TObject>> DataAccessor(TIndex start, TIndex end);

    public delegate void ObjectWritiningFunction(BinaryWriter writer, ref readonly TObject obj);
    public delegate TObject[] ObjectsReadingFunction(BinaryReader reader, int count);
    
    RangesSet<TObject, TIndex> cache;
    DataAccessor getData;
    TIndex mergeDistance;
    public CachingDataAccessor(DataAccessor accessorFunc, ReadWriteSettings? settings = null, TIndex mergeDistance = default)
    {
        cache = new RangesSet<TObject, TIndex>(settings);
        getData = accessorFunc ?? throw new ArgumentNullException(nameof(accessorFunc));
        this.mergeDistance = TIndex.Max(TIndex.One, mergeDistance);
    }

    private class EnumeratorAccessor : IEnumerable<Range<TObject, TIndex>>
    {
        private RangesSet<TObject, TIndex> enumerable;
        public EnumeratorAccessor(RangesSet<TObject, TIndex> enumerable)
        {
            this.enumerable = enumerable;
        }
        public IEnumerator<Range<TObject, TIndex>> GetEnumerator()
        {
            return enumerable.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    /// <summary>
    /// Doesn't close stream. Caller is responsible for <paramref name="stream"/> closing.
    /// <see cref="BinaryWriter"/> is initialized to use <see cref="Encoding.UTF8"/>
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="writingFunc"></param>
    /// <param name="encoding"><see cref="Encoding.UTF8"/> is used if unspecified</param>
    public void SerializeCache(Stream stream, ObjectWritiningFunction writingFunc, Encoding? encoding = null)
    {
        using(var writer = new BinaryWriter(stream, encoding ?? Encoding.UTF8, false)) 
        {
            writer.Write((int)cache.SegmentsCount);
            foreach(var range in new EnumeratorAccessor(cache))
            {
                writer.Write((int)range.ValuesRef.Length);
                for (int i = 0; i < range.ValuesRef.Length; i++)
                {
                    writingFunc(writer, in range.ValuesRef[i]);
                }
            }
        }
    }


    /// <summary>
    /// Discards existing cache
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="readingFunc"></param>
    /// <param name="encoding"></param>
    /// <returns>this object</returns>
    public CachingDataAccessor<TObject, TIndex> Deserialize(Stream stream, ObjectsReadingFunction readingFunc, Encoding? encoding = null)
    {
        RangesSet<TObject, TIndex> newCache = new RangesSet<TObject, TIndex>(cache.settings);
        using(var reader = new BinaryReader(stream, encoding ?? Encoding.UTF8, false))
        {
            int segmentsCount = reader.ReadInt32();
            for (int i = 0; i < segmentsCount; i++)
            {
                int segmentLenght = reader.ReadInt32();
                var segment = readingFunc(reader, segmentLenght);
                newCache.AddRange(segment);
            }
        }
        cache = newCache;
        return this;
    }

    public async Task<ReadOnlySequence<TObject>> GetRange(TIndex start, TIndex end)
    {
        var gaps = cache.GetGaps(start, end);
        foreach(var gap in gaps)
        { 
            var data = await getData(gap.Min, gap.Max);
            cache.AddRange(data.ToArray(), mergeDistance);
        }

        return cache.GetRange(start, end);
    }
}

namespace SeriesCache;

internal class Index<TKey, TValue>
    where TKey : struct, IComparable<TKey>
{
    ReadWriteSettings defaultSettings;
    public Index(ReadWriteSettings? settings)
    {
        defaultSettings = settings ?? new ReadWriteSettings();
    }

    public void Add(TKey key, TValue value, ReadWriteSettings? settings = null)
    { 
        settings = settings ?? defaultSettings;
        throw new NotImplementedException();
    }
}

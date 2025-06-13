using System.Collections;
using System.Numerics;

namespace SeriesCache;

public partial class AVLTree<T, TNumber> : IReadOnlyCollection<T>
    where TNumber : IBinaryInteger<TNumber>, ISignedNumber<TNumber>, IConvertible
{
    public readonly DistanceWithValueRef<T, T, TNumber> DistanceFunc;
    public AVLTree(DistanceWithValueRef<T, T, TNumber> distanceFunc)
    {
        DistanceFunc = distanceFunc;
    }

    internal Node? Root {get; private set; }

    public bool IsEmpty => Root is null;
    public int Count {get; private set;}

    public void Add(T item)
    {
        if (Root is null)
            Root = new Node(item, this, null);
        else
            Root = Root.Add(item);
        ++Count;
    }

    public void Clear()
    {
        Root = null;
        Count = 0;
    }

    public Node GetMin()
    {
        if (IsEmpty)
            throw new InvalidOperationException("No items to search");
        return Root!.FindMostLeft();
    }

    public Node GetMax()
    {
        if (IsEmpty)
            throw new InvalidOperationException("No items to search");
        return Root!.FindMostRight();
    }

    public IEnumerator<T> GetEnumerator()
    {
        if (Root is null)
            yield break;

        foreach(T item in Root)
            yield return item;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

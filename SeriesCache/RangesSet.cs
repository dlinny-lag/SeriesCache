using System.Numerics;
using System.Buffers;

namespace SeriesCache;

internal class RangesSet<TObject, TIndex>
    where TObject : struct
    where TIndex : unmanaged, IBinaryInteger<TIndex>, ISignedNumber<TIndex>, IMinMaxValue<TIndex>, IConvertible
{
    private sealed class Tree : AVLTree<Range<TObject, TIndex>, TIndex>
    {
        private static readonly DistanceWithValueRef<Range<TObject, TIndex>, Range<TObject, TIndex>, TIndex> distanceFunc = 
            (ref readonly Range<TObject, TIndex> item, ref readonly Range<TObject, TIndex> value) => item.Center - value.Center;
        public Tree() : base(distanceFunc)
        {
        }
    }
    private readonly Tree ranges = [];

    internal readonly ReadWriteSettings settings;
    public RangesSet(ReadWriteSettings? settings = null)
    {
        this.settings = settings ?? new ReadWriteSettings();
        Min = TIndex.MaxValue;
        Max = TIndex.MinValue;
    }

    public bool IsEmpty => ranges.IsEmpty;
    public TIndex Min {get; private set;}
    public TIndex Max {get; private set;}

    public int SegmentsCount => ranges.Count;

    public void Clear() => ranges.Clear();

    /// <summary>
    /// Adds a range to the set. If adding range and existing one(s) are instersecting they will be merged for further performance improvement
    /// </summary>
    /// <param name="objects"></param>
    /// <param name="mergeDistance">Merge ranges if distance between them is less or equal to <paramref name="mergeDistance"/></param>
    public void AddRange(Span<TObject> objects, TIndex mergeDistance = default)
    {
        var adding = new Range<TObject, TIndex>(objects);
        if (!ranges.IsEmpty)
        {
            Tree.Node? start = Find(adding.Min);
            Tree.Node? end = Find(adding.Max);

            List<Tree.Node> mergedNodes = new List<Tree.Node>();
            do
            {
                // is current range intersects adding range or is one of ranges inside of another range?
                var testResult = adding.TestInstersection(start.Value);
                if (testResult.IsIntersection(mergeDistance))
                {
                    adding = adding.Merge(start.Value, settings.OnOverwrite);
                    mergedNodes.Add(start);
                }
                if (start == end)
                    break;

                start = start.Next();
            }
            while (start is not null);

            foreach (Tree.Node node in mergedNodes)
            {
                node.Remove();
            }
        }

        ranges.Add(adding);

        if (adding.Min < Min)
            Min = adding.Min;
        if (adding.Max > Max)
            Max = adding.Max;
    }

    class Segment : ReadOnlySequenceSegment<TObject>
    {
        public Segment(ReadOnlyMemory<TObject> memory)
        {
            Memory = memory;
        }

        public Segment Append(ReadOnlyMemory<TObject> memory)
        {
            var nextSegment = new Segment(memory)
            {
                RunningIndex = RunningIndex + Memory.Length
            };

            Next = nextSegment;

            return nextSegment;
        }
    }

    public ReadOnlySequence<TObject> GetRange(TIndex start, TIndex end)
    {
        var startNode = Find(start);
        var endNode = Find(end);
        var currentNode = startNode; 
        Segment startSegment = new Segment(startNode.Value.Values);
        int startIndex = startNode.Value.PositionOf(start) ?? 0; // TODO: extrapolation
        Segment currentSegment = startSegment;
        while (currentNode != endNode)
        {
            currentNode = currentNode.Next();
            if (currentNode is null)
                break; // TODO: extrapolation
            currentSegment = currentSegment.Append(currentNode.Value.Values);
        }
        int? endIndex = endNode.Value.PositionOf(end);
        if (endIndex == -1 || endIndex is null)
            endIndex = endNode.Value.Values.Length-1;
        // TODO: interpolation
        ReadOnlySequence<TObject> retVal = new ReadOnlySequence<TObject>(startSegment, Math.Max(0, startIndex), currentSegment, endIndex.Value + 1);
        return retVal;
    }

    private Tree.Node Find(TIndex index)
    {
        DistanceWithoutValueReadonly<Range<TObject, TIndex>, TIndex> distanceFunc 
            = (ref readonly Range<TObject, TIndex> item) => 
            { 
                var rangeScope = index.IsInRange(item.Min, item.Max);
                switch(rangeScope)
                {
                    case InRange.In:
                        return TIndex.Zero;
                    case InRange.BelowLower:
                        return index - item.Min; // TODO: why it gives same result as item.Min - index? missing test?
                    case InRange.AboveHigher:
                        return index - item.Max;
                }
                throw new NotImplementedException();
            };
        return ranges.FindNearest(distanceFunc);
    }

    /// <summary>
    /// Ranges list that are not in this set. 
    /// Min/Max of returning ranges do not intersect Min/Max of ranges in this ranges set
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public Range<TIndex>[] GetGaps(TIndex start, TIndex end)
    {
        if (start > end) 
            throw new ArgumentException($"start must be less or equal to end, but {start} > {end}");

        if (IsEmpty || end < Min || Max < start)
            return [new Range<TIndex>(start, end)];

        List<Range<TIndex>> retVal = new List<Range<TIndex>>(4);
        var startNode = Find(start);
        var curNode = startNode;
        var currentMin = start;
        while (curNode is not null && curNode.Value.Min < end)
        {
            if (currentMin < curNode.Value.Min)
            {
                retVal.Add(new Range<TIndex>(currentMin + TIndex.One, curNode.Value.Min - TIndex.One));
            }

            currentMin = curNode.Value.Max;
            curNode = curNode.Next();
        }

        if (curNode is null && Max < end)
            retVal.Add(new Range<TIndex>(Max + TIndex.One, end));
        else if (currentMin < end)
            retVal.Add(new Range<TIndex>(currentMin + TIndex.One, end));
        
        return retVal.ToArray();
    }

    internal IEnumerator<Range<TObject, TIndex>> GetEnumerator()
    {
        return ranges.GetEnumerator();
    }
}

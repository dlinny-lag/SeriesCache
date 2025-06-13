using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SeriesCache;

internal sealed partial class Range<TObject, TIndex>
    where TObject : struct
    where TIndex : unmanaged, IBinaryInteger<TIndex>, ISignedNumber<TIndex>, IMinMaxValue<TIndex>, IConvertible
{
    private struct SegmentInfo
    {
        /// <summary>
        /// index of element to process inside segment
        /// <para>can be negative</para>
        /// </summary>
        public int Index;
        /// <summary>
        /// just cache of segement[this.Index]
        /// </summary>
        public TIndex Value;
        /// <summary>
        /// is segment out of elements
        /// </summary>
        public bool Finished;
    }

    /// <summary>
    /// returns index of segment that refer a min value.
    /// returns -1 if no more unprocessed elements 
    /// </summary>
    /// <param name="current"></param>
    /// <param name="segments"></param>
    /// <returns></returns>
    static int FindFirstMin(ref Span<SegmentInfo> current)
    {
        int retVal = -1;
        TIndex min = TIndex.MaxValue;
        int firstWithValue = -1;
        for (int i = 0; i < current.Length; i++)
        {
            if (current[i].Finished)
                continue;
            firstWithValue = i;
            if (current[i].Value < min)
            {
                min = current[i].Value;
                retVal = i;
            }
        }

        if (retVal < 0 && firstWithValue >= 0 && min == TIndex.MaxValue)
            retVal = firstWithValue; // edge case, when there is only MaxValue remain

        return retVal;
    }
    
    static int FindNextMin(ref Span<SegmentInfo> current, TObject[][] segments, int currentMinIndex)
    {
        ref SegmentInfo min = ref current[currentMinIndex];
            
        // find a segment that has element next to min value
        // found element can be identical to current min
        int nextMinIndex = -1;
        for (int i = 0; i < segments.Length; i++)
        {
            if (i == currentMinIndex)
                continue;
            if (current[i].Finished)
                continue;
            if (current[i].Value >= min.Value)
            {
                nextMinIndex = i;
                break;
            }
        }
        return nextMinIndex;
    }

    static void AppendDuplicates(ref Span<SegmentInfo> current, TIndex value, List<int> duplicationSegments)
    {
        var initialDuplicates = duplicationSegments.ToArray().AsSpan();
        for(int i = 0; i < current.Length; i++) 
        {
            if (initialDuplicates.Contains(i))
                continue;
            if (current[i].Finished)
                continue;
            if (current[i].Value == value)
                duplicationSegments.Add(i);
        }
    }

    static TIndex AdvanceByOne(ref SegmentInfo curInfo, TObject[] segment)
    {
        ++curInfo.Index;

        if (curInfo.Index >= segment.Length)
        {
            curInfo.Finished = true;
        }
        else
        {
            TIndex newValue = IndexAccessor.GetIndex<TObject, TIndex>(ref segment[curInfo.Index]);
            if (newValue < curInfo.Value)
                throw new ArgumentException("All segements must be ordered ascending", "segments"); // argument of ctr
            curInfo.Value = newValue;
        }
        return curInfo.Value;
    }

    /// <summary>
    /// Note: segment's ranges can be intersecting
    /// </summary>
    /// <param name="segments"></param>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public Range(OverwriteMode mode = default, params TObject[][] segments)
    {
        if (!IndexAccessor.IsRegistered<TObject, TIndex>())
            throw new InvalidOperationException("Index filed accessor is not initialized");
        
        Span<SegmentInfo> current = stackalloc SegmentInfo[segments.Length];

        Min = TIndex.MaxValue;
        int totalLegth = 0;
        int currentMinIndex = -1;
        for (int s = 0; s < segments.Length; s++)
        {
            if (segments[s].Length > 0)
            {
                TIndex min = IndexAccessor.GetIndex<TObject, TIndex>(ref segments[s][0]);
                current[s].Index = 0;
                current[s].Value = min;
                ++totalLegth;
                if (min < Min) // oldest segment should be picked, so strictly less check
                {
                    currentMinIndex = s;
                    Min = min;
                }
            }
            else
            {
                current[s].Index = -1;
                current[s].Finished = true;
            }
        }
        
        if (totalLegth == 0)
            throw new ArgumentException("At least one element is required", nameof(segments));


        List<TObject> result = new List<TObject>(totalLegth);
        do 
        {
            ref SegmentInfo min = ref current[currentMinIndex];
            
            // find a segment that has element next to min value
            // found element can be identical to current min
            int nextMinIndex = FindNextMin(ref current, segments, currentMinIndex);
            if (nextMinIndex < 0)
            {
                // it is possible that some remaining segment is not correctly ordered, so check it
                for (int i = 0; i < current.Length; i++)
                {
                    if (i == currentMinIndex)
                        continue; // some elements in current segment may remain uncopied
                    if (!current[i].Finished)
                        throw new ArgumentException("All segements must be ordered ascending", nameof(segments));
                }
                // copy the rest of the current segment, it is the last segment to proceed
                if (!min.Finished)
                {
                    var span = segments[currentMinIndex].AsSpan();
                    var rest = span.Slice(min.Index, span.Length-min.Index);
                    result.AddRange(rest);
                    Max = IndexAccessor.GetIndex<TObject, TIndex>(ref rest[rest.Length-1]);
                }
// exit point
                break; // no more unprocessed elements in segments
            }

            // copy elements from current min segment to the result...

            if (min.Value == current[nextMinIndex].Value) // duplication found
            {
                // it is possible that found duplication is not the only one.
                // store all duplications in a list
                List<int> duplicationSegments = new List<int>{currentMinIndex, nextMinIndex};
               
                switch(mode)
                {
                    case OverwriteMode.Error:
                        throw new ArgumentException("No duplications allowed", nameof (segments));
                    case OverwriteMode.Replace:
                        { 
                            AppendDuplicates(ref current, min.Value, duplicationSegments);
                            // pick element from newest segment, i.e. with highest index
                            int dupIndex = duplicationSegments.Max();
                            result.Add(segments[dupIndex][current[dupIndex].Index]);
                        }
                        break;
                    case OverwriteMode.Skip:
                        {
                            AppendDuplicates(ref current, min.Value, duplicationSegments);
                            // pick element from oldest segment, i.e. with lowest index
                            int dupIndex = duplicationSegments.Min();
                            result.Add(segments[dupIndex][current[dupIndex].Index]);
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(mode));
                }
                
                // advance for all duplicates
                TIndex newMin = TIndex.MaxValue;
                foreach(int segmentIndex in duplicationSegments)
                {
                    TIndex newVal = AdvanceByOne(ref current[segmentIndex], segments[segmentIndex]);
                    if (newVal > Max)
                        Max = newVal;
                    if (newVal < newMin) // oldest segment should be picked, so strictly less check
                    {
                        newMin = newVal;
                        if (!current[segmentIndex].Finished)
                            currentMinIndex = segmentIndex;
                    }
                }
                if (newMin == TIndex.MaxValue) // heh, the only MaxValue remain
                    currentMinIndex = duplicationSegments.Min(); // oldest segment should be picked

                continue;
            }

            // no duplications, so some range can be copied to the result list
            ref SegmentInfo nextMin = ref current[nextMinIndex];
            var minSegment = segments[currentMinIndex];
            TIndex newIndex;
            do
            {
                result.Add(minSegment[min.Index]);
                newIndex = AdvanceByOne(ref min, minSegment);
                if (newIndex > Max)
                    Max = newIndex;
            }
            while(!min.Finished && newIndex < nextMin.Value); // do not copy identical value, it will be handled on the next iteration

            currentMinIndex = nextMinIndex;
        }
        while (true);


        _objects = result.ToArray();
    }
}

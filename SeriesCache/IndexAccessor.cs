using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SeriesCache;

public unsafe delegate TIndex CalculateDistanceUnsafe<TIndex>(TIndex* value) where TIndex : unmanaged, IBinaryInteger<TIndex>;

public static class IndexAccessor
{
    [DebuggerStepThrough]
    public static void Register<TObject, TIndex>(int indexFieldOffset) 
        where TObject : struct
        where TIndex : unmanaged, IBinaryInteger<TIndex>
    {
        IndexFieldDescriptor<TObject,TIndex>.Register(indexFieldOffset);
    }

    [DebuggerStepThrough]
    public static bool IsRegistered<TObject, TIndex>()
        where TObject : struct
        where TIndex : unmanaged, IBinaryInteger<TIndex>
    {
        return IndexFieldDescriptor<TObject,TIndex>.IsRegistered();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerStepThrough]
    public static TIndex GetIndex<TObject, TIndex>(ref TObject obj)
        where TObject : struct
        where TIndex : unmanaged, IBinaryInteger<TIndex>
    {
        return IndexFieldDescriptor<TObject,TIndex>.GetIndex(ref obj);
    }

    /// <summary>
    /// Returns found offset of an index field inside <typeparamref name="TObject"/> structure
    /// </summary>
    /// <typeparam name="TIndex"></typeparam>
    /// <param name="fieldsPath"></param>
    /// <param name="ignoreSigness"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static int FindOffset<TObject, TIndex>(string fieldsPath, bool ignoreSigness = false) 
        where TObject : struct
        where TIndex : struct, IBinaryInteger<TIndex>
    {
        string[] fields = fieldsPath.Split('.');
        Type lastOwner = typeof(TObject);
        long offset = 0;
        FieldInfo? fieldInfo = null;
        for (int i = 0; i < fields.Length; i++) 
        {
            if (!lastOwner.IsValueType)
                throw new ArgumentException($"Can't find offset in non value type object {lastOwner.FullName}", nameof(fieldsPath));
            fieldInfo = lastOwner.GetField(fields[i], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (fieldInfo is null)
                throw new ArgumentException($"Can't find {fields[i]} in {lastOwner.FullName} ", nameof(fieldsPath));
            
            // Note: Marshal.OffsetOf works incorrectly

            var pairs = MemoryInspectionHelper.GetFieldOffsets(lastOwner);
            long currentOffset =  pairs.First(pair => pair.fieldInfo == fieldInfo).offset;
            
            offset += currentOffset;
            lastOwner = fieldInfo.FieldType;
        }

        if (lastOwner != typeof(TIndex))
        {
            if (!ignoreSigness || Marshal.SizeOf<TIndex>() != Marshal.SizeOf(lastOwner))
                throw new ArgumentException($"Type mismatch.Expected {typeof(TIndex).FullName}, but found {lastOwner.FullName}");
        }

        return (int)offset;
    }

    public static int BinarySearchUnsafe<TObject, TIndex>(this TObject[] objects, TIndex index)
        where TObject : struct
        where TIndex : unmanaged, IBinaryInteger<TIndex>
    {
        return IndexFieldDescriptor<TObject, TIndex>.BinarySearch(objects, index);
    }

    private readonly record struct Field(int Offset, int Size);

    private static class IndexFieldDescriptor<TObject, TIndex>
        where TObject : struct
        where TIndex : unmanaged, IBinaryInteger<TIndex>
    {
        static Field descriptor;
        static int TObjectSize = MemoryInspectionHelper.GetSizeOfValueTypeInstance<TObject>();
        public unsafe static void Register(int indexFieldOffset)
        {
            if (descriptor.Size == 0)
                descriptor = new Field(indexFieldOffset, sizeof(TIndex));
            else
            {
                if (indexFieldOffset != descriptor.Offset)
                    throw new InvalidOperationException("Attempt to overwrite offset with different value");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsRegistered() => descriptor.Size != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsNotRegistered() => descriptor.Size == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static TIndex GetIndex(ref TObject obj)
        {
            if (IsNotRegistered())
                throw new InvalidOperationException($"No index offset registered for {typeof(TObject).FullName}");
            unsafe 
            {
                byte* mem = (byte*)Unsafe.AsPointer(ref obj);
                mem += descriptor.Offset;
                return *(TIndex*)mem;
            }
        }

        public unsafe static int BinarySearch(TObject[] values, TIndex search)
        {
            if (IsNotRegistered())
                throw new InvalidOperationException($"No index offset registered for {typeof(TObject).FullName}");

            int mid, first = 0, last = values.Length-1;
#pragma warning disable 8500
            fixed(TObject* arrayStart = &values[0])
            {
                byte* start = ((byte*)arrayStart) + descriptor.Offset;

                while (first <= last)
                {
                    mid = first + ((last - first) >> 1);

                    TIndex* ptr = (TIndex*)(start + mid*TObjectSize);
                    TIndex compareResult = search - *ptr;

                    if (compareResult == TIndex.Zero) 
                        return mid;
                    if (compareResult < TIndex.Zero)
                        last = mid - 1;
                    else
                        first = mid + 1;
                }
                return ~first;
            }
#pragma warning restore 8500

        }
    }
}

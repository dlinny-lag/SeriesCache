using System.Reflection.Emit;
using System.Reflection;
using System.Text;

namespace SeriesCache;

internal static class MemoryInspectionHelper
{
    // see https://devblogs.microsoft.com/premier-developer/managed-object-internals-part-4-fields-layout/

    public static string Layout<T>(ref readonly T obj) where T : struct
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"{typeof(T).FullName}: Size = {GetSizeOfValueTypeInstance<T>()}");
        var offsets = GetFieldOffsets(in obj);
        foreach (var pair in offsets)
        {
            sb.AppendLine($"Field {pair.fieldInfo.Name}: starts at offset {pair.offset}");
        }

        return sb.ToString();
    }

    public static (FieldInfo fieldInfo, int offset)[] GetFieldOffsets<T>() where T: struct
    {
        T dummy = default;
        return GetFieldOffsets<T>(ref dummy);
    }

    public static (FieldInfo fieldInfo, int offset)[] GetFieldOffsets(Type type)
    {
        if (!type.IsValueType)
            throw new ArgumentException("Value type is required", nameof(type));
        var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
        if (fields.Length == 0)
        {
            return Array.Empty<(FieldInfo, int)>();
        }

        Func<object, long[]> fieldOffsetInspector = GenerateFieldOffsetInspectionFunction(fields);

        var addresses = fieldOffsetInspector(Activator.CreateInstance(type)!);
        var baseLine = addresses.Min();
            
        // Converting field addresses to offsets using the first field as a baseline
        return fields
            .Select((field, index) => (field: field, offset: (int)(addresses[index] - baseLine)))
            .OrderBy(tuple => tuple.offset)
            .ToArray();
    }

    private static (FieldInfo fieldInfo, int offset)[] GetFieldOffsets<T>(ref readonly T obj)
    {
        var fields = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
        if (fields.Length == 0)
        {
            return Array.Empty<(FieldInfo, int)>();
        }
        Func<object, long[]> fieldOffsetInspector = GenerateFieldOffsetInspectionFunction(fields);

        var addresses = fieldOffsetInspector(obj!);
        var baseLine = addresses.Min();
            
        // Converting field addresses to offsets using the first field as a baseline
        return fields
            .Select((field, index) => (field: field, offset: (int)(addresses[index] - baseLine)))
            .OrderBy(tuple => tuple.offset)
            .ToArray();
    }

    private static Func<object, long[]> GenerateFieldOffsetInspectionFunction(params FieldInfo[] fields)
    {
        var method = new DynamicMethod(
            name: "GetFieldOffsets",
            returnType: typeof(long[]),
            parameterTypes: new[] { typeof(object) },
            m: typeof(MemoryInspectionHelper).Module,
            skipVisibility: true);

        ILGenerator ilGen = method.GetILGenerator();

        // Declaring local variable of type long[]
        ilGen.DeclareLocal(typeof(long[]));
        // Loading array size onto evaluation stack
        ilGen.Emit(OpCodes.Ldc_I4, fields.Length);

        // Creating an array and storing it into the local
        ilGen.Emit(OpCodes.Newarr, typeof(long));
        ilGen.Emit(OpCodes.Stloc_0);

        for (int i = 0; i < fields.Length; i++)
        {
            // Loading the local with an array
            ilGen.Emit(OpCodes.Ldloc_0);

            // Loading an index of the array where we're going to store the element
            ilGen.Emit(OpCodes.Ldc_I4, i);

            // Loading object instance onto evaluation stack
            ilGen.Emit(OpCodes.Ldarg_0);

            // Getting the address for a given field
            ilGen.Emit(OpCodes.Ldflda, fields[i]);

            // Converting field offset to long
            ilGen.Emit(OpCodes.Conv_I8);

            // Storing the offset in the array
            ilGen.Emit(OpCodes.Stelem_I8);
        }

        ilGen.Emit(OpCodes.Ldloc_0);
        ilGen.Emit(OpCodes.Ret);

        return (Func<object, long[]>)method.CreateDelegate(typeof(Func<object, long[]>));
    }

    private struct SizeComputer<T>
    {
#pragma warning disable 0649
        public T DummyField;
        public T OffsetField;
#pragma warning restore 0649
        //public SizeComputer(T dummyField, T offset) => (this.dummyField, this.offset) = (dummyField, offset);
    }
    public static int GetSizeOfValueTypeInstance<T>() where T : struct
    {
        // The offset of the second field is the size of the 'type'
        var fieldsOffsets = GetFieldOffsets<SizeComputer<T>>();
        return fieldsOffsets[1].offset;
    }
}

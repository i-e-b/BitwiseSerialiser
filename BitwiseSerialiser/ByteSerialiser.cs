using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;

namespace BitwiseSerialiser;

/// <summary>
/// Special purpose serialiser for extracting and packing communication details for EWCs
/// </summary>
public static class ByteSerialiser
{
    /// <summary>
    /// De-serialiser won't create variable-sized array items larger than this.
    /// </summary>
    public const int VariableByteStringSafetyLimit = 10240;
        
    /// <summary>
    /// Serialise a [ByteLayout] object to a byte array
    /// </summary>
    /// <param name="source">Object to be serialised</param>
    /// <typeparam name="T">Source type. Must be marked with the [ByteLayout] attribute, and obey the rules of the attribute</typeparam>
    /// <returns>Byte array representation of the source</returns>
    public static byte[] ToBytes<T>(T source)
    {
        // Plan:
        // 1. start an empty list
        // 2. get all BigEndianAttribute fields recursively, ordered appropriately
        // 3. run through each field and pull a value (assign to UInt64 and shift?)
        // 4. return the list.ToArray()
        var output = new ByteWriter();

        SerialiseObjectRecursive(source, output);

        return output.ToArray();
    }

    /// <summary>
    /// Deserialise a byte array into a [ByteLayout] object.
    /// If the byte array is too short to fill the object, a partially complete object is returned.
    /// </summary>
    /// <param name="source">Byte array to be deserialised</param>
    /// <param name="result">New instance of T</param>
    /// <typeparam name="T">Target type. Must be marked with the [ByteLayout] attribute, and obey the rules of the attribute</typeparam>
    /// <returns>True if source was long enough to complete the result, false if too short. Returns true if source is longer than needed.</returns>
    public static bool FromBytes<T>(IEnumerable<byte> source, out T result) where T : new()
    {
        var data = source.ToArray();
        var ok = FromBytes(typeof(T), data, 0, data.Length, out var obj);
        result = (T)obj;
        return ok;
    }

    /// <summary>
    /// Deserialise a byte array into a [ByteLayout] object.
    /// If the byte array is too short to fill the object, a partially complete object is returned.
    /// </summary>
    /// <param name="source">Byte array to be deserialised</param>
    /// <param name="result">New instance of T</param>
    /// <param name="offset">Offset into source to start reading</param>
    /// <param name="length">Maximum length to read from source</param>
    /// <typeparam name="T">Target type. Must be marked with the [ByteLayout] attribute, and obey the rules of the attribute</typeparam>
    /// <returns>True if source was long enough to complete the result, false if too short. Returns true if source is longer than needed.</returns>
    public static bool FromBytes<T>(IEnumerable<byte> source, int offset, int length, out T result) where T : new()
    {
        var data = source.ToArray();
        var ok = FromBytes(typeof(T), data, offset, length, out var obj);
        result = (T)obj;
        return ok;
    }

    /// <summary>
    /// Deserialise a byte array into a [ByteLayout] object.
    /// If the byte array is too short to fill the object, a partially complete object is returned.
    /// </summary>
    /// <param name="type">Target type. Must be marked with the [ByteLayout] attribute, and obey the rules of the attribute</param>
    /// <param name="source">Byte array to be deserialised</param>
    /// <param name="length">Maximum length to read from source</param>
    /// <param name="result">New instance of T</param>
    /// <param name="offset">Offset into source to start reading</param>
    /// <returns>True if source was long enough to complete the result, false if too short. Returns true if source is longer than needed.</returns>
    public static bool FromBytes(Type type, byte[] source, int offset, int length, out object result)
    {
        // Plan:
        // 1. get all BigEndianAttribute fields recursively, ordered appropriately
        // 2. run through each field and pull a value (increment bytes with shift?)
        // 3. return the result type
        result = Activator.CreateInstance(type) ?? throw new Exception($"Failed to create instance of {type.Name}");
        var feed = new RunOutByteSource(source, offset, length);

        RestoreObjectRecursive(feed, ref result);

        return !feed.WasOverRun;
    }

    private static void SerialiseObjectRecursive(object? source, ByteWriter output)
    {
        if (source is null) return;
        var publicFields = source.GetType()
            .GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Where(NotSpecial)
            .OrderBy(AttributeOrder)
            .ToList();

        foreach (var field in publicFields)
        {
            SerialiseFieldRecursive(source, field, output);
        }
    }

    private static void SerialiseFieldRecursive(object? source, FieldInfo field, ByteWriter output)
    {
        if (source is null) return;
        int byteCount;

        if (IsFixedValue(field, out var bytes))
        {
            // Check we have a matching field definition
            if (IsBigEnd(field, out byteCount)) {
                if (bytes.Length != byteCount) throw new Exception($"Mismatch between {nameof(BigEndianAttribute)} and {nameof(FixedValueAttribute)} definition in {field.DeclaringType?.Name}.{field.Name}");
                for (int i = 0; i < bytes.Length; i++) output.Add(bytes[i]);
            } else if (IsLittleEnd(field, out byteCount)) {
                if (bytes.Length != byteCount) throw new Exception($"Mismatch between {nameof(LittleEndianAttribute)} and {nameof(FixedValueAttribute)} definition in {field.DeclaringType?.Name}.{field.Name}");
                for (int i = bytes.Length-1; i >= 0; i--) output.Add(bytes[i]);
            } else if (IsByteString(field, out byteCount)) {
                if (bytes.Length != byteCount) throw new Exception($"Mismatch between {nameof(ByteStringAttribute)} and {nameof(FixedValueAttribute)} definition in {field.DeclaringType?.Name}.{field.Name}");
                for (int i = 0; i < bytes.Length; i++) output.Add(bytes[i]);
            } else {
                if (bytes.Length != byteCount) throw new Exception($"Field {field.DeclaringType?.Name}.{field.Name} with {nameof(FixedValueAttribute)} must also have one of {nameof(BigEndianAttribute)}, {nameof(LittleEndianAttribute)}, {nameof(ByteStringAttribute)}, {nameof(AsciiStringAttribute)}");
                for (int i = 0; i < bytes.Length; i++) output.Add(bytes[i]);
            }
            return;
        }

        if (IsBigEnd(field, out byteCount))
        {
            output.WriteBytesBigEnd(GetValueAsInt(source, field), byteCount);
            return;
        }
            
        if (IsPartialBigEnd(field, out var bitCount))
        {
            var intValues = GetValueAsInt(source, field);
            output.WriteBitsBigEndian(intValues, bitCount);
            return;
        }

        if (IsLittleEnd(field, out byteCount))
        {
            output.WriteBytesLittleEnd(GetValueAsInt(source, field), byteCount);
            return;
        }

        if (IsByteString(field, out byteCount) || IsAsciiString(field, out byteCount)) // if value is longer than declared, we truncate
        {
            var byteValues = GetValueAsByteArray(source, field);
                
            // If value is shorter than declared, we pad
            var pad = byteCount - byteValues.Length;
            for (int i = 0; i < pad; i++)
            {
                output.Add(0);
            }
                
            var idx = 0;
            for (var i = pad; i < byteCount; i++)
            {
                output.Add(byteValues[idx++]);
            }

            return;
        }

        if (IsVariableByteString(field, out var variableLengthName)) // We check the declared length against actual
        {
            var byteValues = GetValueAsByteArray(source, field);
            
            var expectedLength = GetLengthFromNamedFunction(field, source, variableLengthName);
            if (byteValues.Length != expectedLength) throw new Exception($"Variable byte string declares {expectedLength} bytes, but {byteValues.Length} bytes were supplied");
            
            for (var i = 0; i < byteValues.Length; i++)
            {
                output.Add(byteValues[i]);
            }

            return;
        }
        
        if (IsValueTerminatedByteString(field, out var stopValue)) // We check the declared length against actual
        {
            var byteValues = GetValueAsByteArray(source, field);

            for (var i = 0; i < byteValues.Length; i++)
            {
                output.Add(byteValues[i]);
            }

            // if the value doesn't have the stop in place, append it.
            if (byteValues[byteValues.Length - 1] != stopValue) output.Add(stopValue);

            return;
        }
            
        if (IsRemainingBytes(field)) // Write all bytes
        {
            var byteValues = GetValueAsByteArray(source, field);
            for (var i = 0; i < byteValues.Length; i++)
            {
                output.Add(byteValues[i]);
            }

            return;
        }

        if (IsChildType(field))
        {
            if (IsFixedRepeaterChildType(field, out var repeatCount))
            {
                // Fixed size output. If the source is wrong size we'll throw
                // This is to simplify the case of varying length child elements
                var childSrc = ListOf(field.GetValue(source) as IEnumerable);
                if (childSrc is null) throw new Exception($"{field.DeclaringType?.Name}.{field.Name} should be an enumerable type, but was not");
                if (childSrc.Count != repeatCount) throw new Exception($"{field.DeclaringType?.Name}.{field.Name} should have {repeatCount} items, but has {childSrc.Count}");

                foreach (var child in childSrc)
                {
                    SerialiseObjectRecursive(child, output);
                }
            }
            else if (IsVariableRepeaterChildType(field, out var repeatName)) // We check the declared length against actual
            {
                if (repeatName is null) throw new Exception($"{field.DeclaringType?.Name}.{field.Name} has an invalid function name");
                var expectedRepeatCount = GetLengthFromNamedFunction(field, source, repeatName);
                
                var childSrc = ListOf(field.GetValue(source) as IEnumerable);
                if (childSrc is null) throw new Exception($"{field.DeclaringType?.Name}.{field.Name} should be an enumerable type, but was not");
                if (childSrc.Count != expectedRepeatCount) throw new Exception($"{field.DeclaringType?.Name}.{field.Name} declared {expectedRepeatCount} items, but has {childSrc.Count}");
                
                foreach (var child in childSrc)
                {
                    SerialiseObjectRecursive(child, output);
                }
            }
            else // assume it's a recursive type
            {
                // we need to recurse deeper
                var child = field.GetValue(source);
                SerialiseObjectRecursive(child, output);
            }

            return;
        }
        
        throw new Exception($"Did not find a valid way of handling {field.DeclaringType?.Name}.{field.Name}");
    }

    private static List<object>? ListOf(IEnumerable? enumerable)
    {
        if (enumerable is null) return null;
        var result = new List<object>();
        foreach (var obj in enumerable)
        {
            if (obj is null) continue;
            result.Add(obj);
        }
        return result;
    }

    private static readonly WeakCache<Type, List<FieldInfo>> _publicFieldCache = new(ReadTypeFields);

    private static List<FieldInfo> ReadTypeFields(Type t)
    {
        return t
            .GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Where(NotSpecial)
            .OrderBy(AttributeOrder)
            .ToList();
    }

    /// <summary>
    /// Skip 'special name' fields -- these are used by C# to back flags enums
    /// </summary>
    private static bool NotSpecial(FieldInfo field)
    {
        return !field.IsSpecialName;
    }

    private static void RestoreObjectRecursive(RunOutByteSource feed, ref object output)
    {
        var position = feed.GetPosition();
        RestoreFieldsRecursive(feed, output);

        if (IsByteLayout(output, out var specialiser))
        {
            if (specialiser is null) return; // don't need to specialise
            var specialType = GetSpecialisation(output, specialiser);
            if (specialType is null) return; // this one not different
            
            // rewind the source, make a new output object, fill it again
            feed.ResetTo(position);
            output = Activator.CreateInstance(specialType) ?? throw new Exception($"Failed to create instance of {specialType.Name}");
            RestoreFieldsRecursive(feed, output);
        }
    }

    private static Type? GetSpecialisation(object output, string functionName)
    {
        // Try to find public instance method by name, and check it's valid
        var method = output.GetType().GetMethod(functionName, BindingFlags.Public | BindingFlags.Instance);
        if (method is null) throw new Exception($"No such specialise function '{functionName}' in type {output.GetType().Name}");
        var methodParams = method.GetParameters();
        if (methodParams.Length > 0) throw new Exception($"Invalid specialise function: {output.GetType().Name}.{functionName}({string.Join(", ", methodParams.Select(p => p.Name))}); Specialise functions should have no parameters");
        if (method.ReturnType != typeof(Type)) throw new Exception($"Specialise function {output.GetType().Name}.{functionName}() returns {method.ReturnType.Name}, but should return 'Type'");

        // Call the function to get new type
        return method.Invoke(output, null!) as Type;
    }

    private static void RestoreFieldsRecursive(RunOutByteSource feed, object output)
    {
        var publicFields = _publicFieldCache.Get(output.GetType());

        foreach (var field in publicFields)
        {
            RestoreFieldRecursive(feed, field, output);
        }
    }

    private static void RestoreFieldRecursive(RunOutByteSource feed, FieldInfo field, object output)
    {
        if (IsBigEnd(field, out var byteCount))
        {
            var intValue = 0UL;
            for (var i = byteCount - 1; i >= 0; i--)
            {
                var b = feed.NextByte();
                intValue += (UInt64)b << (i * 8);
            }

            CastAndSetField(field, output, intValue);
            return;
        }

        if (IsPartialBigEnd(field, out var bitCount))
        {
            var intValue = 0UL;

            while (bitCount > 8)
            {
                var b = feed.NextByte();
                intValue = (intValue << 8) + b;
                bitCount -= 8;
            }

            if (bitCount > 0)
            {
                var b = feed.NextBits(bitCount);
                intValue <<= bitCount;
                intValue |= b;
            }

            CastAndSetField(field, output, intValue);
            return;
        }

        if (IsLittleEnd(field, out byteCount))
        {
            var intValue = 0UL;
            for (var i = 0; i < byteCount; i++)
            {
                var b = feed.NextByte();
                intValue += (UInt64)b << (i * 8);
            }

            CastAndSetField(field, output, intValue);
            return;
        }

        if (IsByteString(field, out byteCount))
        {
            var byteValues = new byte[byteCount];
            for (var i = 0; i < byteCount; i++)
            {
                byteValues[i] = feed.NextByte();
            }

            CastAndSetField(field, output, byteValues);
            return;
        }
        if (IsAsciiString(field, out byteCount))
        {
            var byteValues = new byte[byteCount];
            for (var i = 0; i < byteCount; i++)
            {
                byteValues[i] = feed.NextByte();
            }

            CastAndSetField(field, output, byteValues);
            return;
        }
        
        if (IsVariableByteString(field, out var functionName))
        {
            byteCount = GetLengthFromNamedFunction(field, output, functionName);

            // go fetch bytes
            byte[] byteValues;
            if (byteCount < 1 || byteCount > VariableByteStringSafetyLimit)
            {
                byteValues = Array.Empty<byte>();
            }
            else
            {

                byteValues = new byte[byteCount];
                for (var i = 0; i < byteCount; i++)
                {
                    byteValues[i] = feed.NextByte();
                }
            }

            CastAndSetField(field, output, byteValues);
            return;
        }
        
        if (IsValueTerminatedByteString(field, out var stopValue))
        {
            var length = feed.GetRemainingLength();
            var byteValues = new List<byte>();
            for (var i = 0; i < length; i++) // we will stop at the end of input if we don't see the stop value
            {
                var b = feed.NextByte();
                byteValues.Add(b);
                if (b == stopValue) break;
            }

            CastAndSetField(field, output, byteValues.ToArray());
            return;
        }

        if (IsRemainingBytes(field))
        {
            var length = feed.GetRemainingLength();
            var byteValues = new byte[length];
            for (var i = 0; i < length; i++)
            {
                byteValues[i] = feed.NextByte();
            }

            CastAndSetField(field, output, byteValues);
            return;
        }

        if (IsChildType(field))
        {
            if (IsFixedRepeaterChildType(field, out var repeatCount))
            {
                if (!field.FieldType.IsArray) throw new Exception($"Repeater {field.DeclaringType?.Name}.{field.Name} should be an array type");
                
                var coreType = field.FieldType.GetElementType();
                if (coreType is null) throw new Exception($"Could not determine type of repeater {field.DeclaringType?.Name}.{field.Name}. Try declaring as an array");
                    
                var target = Array.CreateInstance(coreType, repeatCount);

                for (int i = 0; i < repeatCount; i++)
                {
                    var child = field.GetValue(output)
                                ?? Activator.CreateInstance(coreType)
                                ?? throw new Exception($"Failed to find or create instance of {field.DeclaringType?.Name}.{field.Name}");

                    RestoreObjectRecursive(feed, ref child);
                    target.SetValue(child,i);
                }
                field.SetValue(output, target);
            }
            else if (IsVariableRepeaterChildType(field, out var repeatFunctionName))
            {
                if (repeatFunctionName is null) throw new Exception($"{field.DeclaringType?.Name}.{field.Name} has an invalid function name");
                if (!field.FieldType.IsArray) throw new Exception($"Repeater {field.DeclaringType?.Name}.{field.Name} should be an array type");
                
                var coreType = field.FieldType.GetElementType();
                if (coreType is null) throw new Exception($"Could not determine type of repeater {field.DeclaringType?.Name}.{field.Name}. Try declaring as an array");
                    
                var declaredCount = GetLengthFromNamedFunction(field, output, repeatFunctionName);
                
                var target = Array.CreateInstance(coreType, declaredCount);

                for (int i = 0; i < declaredCount; i++)
                {
                    var child = Activator.CreateInstance(coreType)
                                ?? throw new Exception($"Failed to find or create instance of {field.DeclaringType?.Name}.{field.Name}");

                    RestoreObjectRecursive(feed, ref child);
                    target.SetValue(child,i);
                }
                field.SetValue(output, target);
            }
            else
            {
                // recurse deeper
                var child = field.GetValue(output)
                            ?? Activator.CreateInstance(field.FieldType)
                            ?? throw new Exception($"Failed to find or create instance of {field.DeclaringType?.Name}.{field.Name}");

                RestoreObjectRecursive(feed, ref child);
                field.SetValue(output, child);
            }
            return;
        }
        
        throw new Exception($"Did not find a valid way of handling {field.DeclaringType?.Name}.{field.Name}");
    }

    private static int GetLengthFromNamedFunction(FieldInfo field, object sourceObject, string functionName)
    {
        // Try to find public instance method by name, and check it's valid
        var method = field.DeclaringType?.GetMethod(functionName, BindingFlags.Public | BindingFlags.Instance);
        if (method is null) throw new Exception($"No such calculation function '{functionName}' in type {field.DeclaringType?.Name}, as declared by its field {field.Name}");
        var methodParams = method.GetParameters();
        if (methodParams.Length > 0) throw new Exception($"Invalid calculator function: {field.DeclaringType?.Name}.{functionName}({string.Join(", ", methodParams.Select(p => p.Name))}); Calculator functions should have no parameters");
        if (method.ReturnType != typeof(int)) throw new Exception($"Calculator function {field.DeclaringType?.Name}.{functionName}() returns {method.ReturnType.Name}, but should return 'int'");

        // Call the function to get length
        return (method.Invoke(sourceObject, null!) as int?) ?? throw new Exception($"Calculator function {field.DeclaringType?.Name}.{functionName}() returned an unexpected value");
    }

    private static bool IsGenericEnumerator(Type fieldFieldType, out object o)
    {
        throw new NotImplementedException();
    }

    private static bool IsList(Type containerType, out object itemType)
    {
        itemType = typeof(object);
        if (containerType.GenericTypeArguments?.Length != 1) return false;
        var expected = typeof(List<>).MakeGenericType(containerType.GenericTypeArguments[0]!);
        if (containerType != expected) return false;
        itemType = containerType.GenericTypeArguments[0]!;
        return true;
    }

    private static void CastAndSetField(FieldInfo field, object output, ulong intValue)
    {
        var t = field.FieldType;

        /**/
        if (t == typeof(byte)) field.SetValue(output, (byte)intValue);
        else if (t == typeof(UInt16)) field.SetValue(output, (UInt16)intValue);
        else if (t == typeof(UInt32)) field.SetValue(output, (UInt32)intValue);
        else if (t == typeof(UInt64)) field.SetValue(output, intValue);
        else if (t == typeof(Int16)) field.SetValue(output, (Int16)intValue);
        else if (t == typeof(Int32)) field.SetValue(output, (Int32)intValue);
        else if (t == typeof(Int64)) field.SetValue(output, (Int64)intValue);
        else if (t.IsEnum)
        {
            field.SetValue(output, Enum.ToObject(t, intValue));
        }
        else throw new Exception($"Unsupported type '{t.Name}' in {field.DeclaringType?.Name}.{field.Name}");
    }

    private static void CastAndSetField(FieldInfo field, object output, byte[] byteValues)
    {
        var t = field.FieldType;

        /**/
        if (t == typeof(byte[])) field.SetValue(output, byteValues);
        else if (t == typeof(string)) field.SetValue(output, Encoding.ASCII.GetString(byteValues));
        else throw new Exception($"Unsupported type '{t.Name}' in {field.DeclaringType?.Name}.{field.Name}");
    }

    private static ulong GetValueAsInt<T>(T source, FieldInfo field)
    {
        if (source is null) return 0UL;
        var val = field.GetValue(source);
        if (val is null) return 0UL;
        var asInt = Convert.ToUInt64(val);
        return asInt;
    }

    private static byte[] GetValueAsByteArray<T>(T source, FieldInfo field)
    {
        if (source is null) return Array.Empty<byte>();
        var val = field.GetValue(source);
        if (val is null) return Array.Empty<byte>();
        if (val is byte[] arr) return arr;
        if (val is string str) return Encoding.ASCII.GetBytes(str);
        return Array.Empty<byte>();
    }

    private static readonly WeakCache<MemberInfo, (bool, byte[])> _isFixedValuesCache = new(CalculateIsFixedValue);
    
    private static bool IsFixedValue(MemberInfo field, out byte[] bytes)
    {
        var (result, data) = _isFixedValuesCache.Get(field);
        bytes = data;
        return result;
    }

    private static (bool, byte[]) CalculateIsFixedValue(MemberInfo field)
    {
        var match = field.CustomAttributes.OrEmpty().FirstOrDefault(a => a.AttributeType == typeof(FixedValueAttribute));
        if (match is null) return (false, Array.Empty<byte>());
        var bytes = match.ConstructorArguments[0].Value as System.Collections.ObjectModel.ReadOnlyCollection<CustomAttributeTypedArgument>;
        if (bytes is null) return (false, Array.Empty<byte>());
        return (true, bytes.Select(v=>(byte)v.Value).ToArray());
    }
    
    private static readonly WeakCache<Type, (bool, string?)> _isByteLayoutCache = new(CalculateIsByteLayout);
    
    private static bool IsByteLayout(object field, out string? attr)
    {
        var (result, data) = _isByteLayoutCache.Get(field.GetType());
        attr = data;
        return result;
    }

    private static (bool, string?) CalculateIsByteLayout(Type obj)
    {
        var match = obj.CustomAttributes.OrEmpty().FirstOrDefault(a => a.AttributeType == typeof(ByteLayoutAttribute));
        if (match is null) return (false, null);
        var param = match.NamedArguments?.Where(a => a.MemberName == nameof(ByteLayoutAttribute.SpecialiseWith)).Select(m=>m.TypedValue.Value.ToString()).FirstOrDefault();
        return (true, param);
    }

    private static readonly WeakCache<MemberInfo, (bool, int)> _isBigEndCache = new(CalculateIsBigEnd);

    private static bool IsBigEnd(MemberInfo field, out int bytes)
    {
        var (result, size) = _isBigEndCache.Get(field);
        bytes = size;
        return result;
    }

    private static (bool, int) CalculateIsBigEnd(MemberInfo field)
    {
        var match = field.CustomAttributes.OrEmpty().FirstOrDefault(a => a.AttributeType == typeof(BigEndianAttribute));
        if (match is null) return (false, 0);
        var byteCount = match.ConstructorArguments[0].Value as int?;
        if (byteCount is null) return (false, 0);
        return (true, byteCount.Value);
    }

    private static readonly WeakCache<MemberInfo, (bool, int)> _isPartialBigEndCache = new(CalculateIsPartialBigEnd);

    private static bool IsPartialBigEnd(MemberInfo field, out int bits)
    {
        var (result, size) = _isPartialBigEndCache.Get(field);
        bits = size;
        return result;
    }

    private static (bool, int) CalculateIsPartialBigEnd(MemberInfo field)
    {
        var match = field.CustomAttributes.OrEmpty().FirstOrDefault(a => a.AttributeType == typeof(BigEndianPartialAttribute));
        if (match is null) return (false, 0);
        var bitCount = match.ConstructorArguments[0].Value as int?;
        if (bitCount is null) return (false, 0);
        return (true, bitCount.Value);
    }

    private static readonly WeakCache<MemberInfo, (bool, int)> _isLittleEndCache = new(CalculateIsLittleEnd);

    private static bool IsLittleEnd(MemberInfo field, out int bytes)
    {
        var (result, size) = _isLittleEndCache.Get(field);
        bytes = size;
        return result;
    }

    private static (bool, int) CalculateIsLittleEnd(MemberInfo field)
    {
        var match = field.CustomAttributes.OrEmpty().FirstOrDefault(a => a.AttributeType == typeof(LittleEndianAttribute));
        if (match is null) return (false, 0);
        var byteCount = match.ConstructorArguments[0].Value as int?;
        if (byteCount is null) return (false, 0);
        return (true, byteCount.Value);
    }

    private static readonly WeakCache<MemberInfo, (bool, int)> _isByteStringCache = new(CalculateIsByteString);

    private static bool IsByteString(MemberInfo field, out int bytes)
    {
        var (result, size) = _isByteStringCache.Get(field);
        bytes = size;
        return result;
    }

    private static (bool, int) CalculateIsByteString(MemberInfo field)
    {
        var match = field.CustomAttributes.OrEmpty().FirstOrDefault(a => a.AttributeType == typeof(ByteStringAttribute));
        if (match is null) return (false, 0);
        var byteCount = match.ConstructorArguments[0].Value as int?;
        if (byteCount is null) return (false, 0);
        return (true, byteCount.Value);
    }
    
    private static readonly WeakCache<MemberInfo, (bool, int)> _isAsciiStringCache = new(CalculateIsAsciiString);

    private static bool IsAsciiString(MemberInfo field, out int bytes)
    {
        var (result, size) = _isAsciiStringCache.Get(field);
        bytes = size;
        return result;
    }

    private static (bool, int) CalculateIsAsciiString(MemberInfo field)
    {
        var match = field.CustomAttributes.OrEmpty().FirstOrDefault(a => a.AttributeType == typeof(AsciiStringAttribute));
        if (match is null) return (false, 0);
        var byteCount = match.ConstructorArguments[0].Value as int?;
        if (byteCount is null) return (false, 0);
        return (true, byteCount.Value);
    }
    
    private static readonly WeakCache<MemberInfo, (bool, byte)> _isValueTerminateByteStringCache = new(CalculateIsValueTerminatedByteString);

    private static bool IsValueTerminatedByteString(MemberInfo field, out byte stopValue)
    {
        var (result, value) = _isValueTerminateByteStringCache.Get(field);
        stopValue = value;
        return result;
    }

    private static (bool, byte) CalculateIsValueTerminatedByteString(MemberInfo field)
    {
        var match = field.CustomAttributes.OrEmpty().FirstOrDefault(a => a.AttributeType == typeof(ValueTerminatedByteStringAttribute));
        if (match is null) return (false, 0);
        var value = match.ConstructorArguments[0].Value as byte?;
        if (value is null) return (false, 0);
        return (true, value.Value);
    }
    
    
    private static readonly WeakCache<MemberInfo, (bool, string)> _isVariableByteStringCache = new(CalculateIsVariableByteString);

    private static bool IsVariableByteString(MemberInfo field, out string functionName)
    {
        var (result, name) = _isVariableByteStringCache.Get(field);
        functionName = name;
        return result;
    }

    private static (bool, string) CalculateIsVariableByteString(MemberInfo field)
    {
        var match = field.CustomAttributes.OrEmpty().FirstOrDefault(a => a.AttributeType == typeof(VariableByteStringAttribute));
        if (match is null) return (false, "");
        var funcName = match.ConstructorArguments[0].Value as string;
        if (funcName is null) return (false, "");
        return (true, funcName);
    }
    
    private static readonly WeakCache<MemberInfo, (bool,int, string?)> _isChildTypeCache = new(CalculateIsChildType);

    private static bool IsChildType(MemberInfo field)
    {
        var (result, _, _) = _isChildTypeCache.Get(field);
        return result;
    }
    
    private static bool IsFixedRepeaterChildType(MemberInfo field, out int repeatCount)
    {
        var (_, count, name) = _isChildTypeCache.Get(field);
        repeatCount = count;
        return (count >= 0) && (name is null);
    }

    private static bool IsVariableRepeaterChildType(MemberInfo field, out string? repeaterName)
    {
        var (_, count, name) = _isChildTypeCache.Get(field);
        repeaterName = name;
        return (count == -1) && (name is not null);
    }

    private static (bool,int, string?) CalculateIsChildType(MemberInfo field)
    {
        var attrs = field.CustomAttributes.OrEmpty();
        foreach (var attr in attrs)
        {
            if (attr.AttributeType == typeof(ByteLayoutChildAttribute)) return (true, -1, null);
            if (attr.AttributeType == typeof(ByteLayoutMultiChildAttribute))
            {
                var count = attr.ConstructorArguments[0].Value as int? ?? 0;
                return (true, count, null);
            }

            if (attr.AttributeType == typeof(ByteLayoutVariableChildAttribute))
            {
                var name = attr.ConstructorArguments[0].Value as string;
                return (true, -1, name);
            }
        }
        return (false,0,null);
    }

    private static readonly WeakCache<MemberInfo, bool> _isRemainingBytesCache = new(CalculateIsRemainingBytes);

    private static bool IsRemainingBytes(MemberInfo field)
    {
        var result = _isRemainingBytesCache.Get(field);
        return result;
    }

    private static bool CalculateIsRemainingBytes(MemberInfo field)
    {
        var match = field.CustomAttributes.OrEmpty().FirstOrDefault(a => a.AttributeType == typeof(RemainingBytesAttribute));
        if (match is null) return false;
        return true;
    }

    private static readonly WeakCache<FieldInfo, int> _fieldOrderCache = new(CalculateAttributeOrder);

    private static int AttributeOrder(FieldInfo field)
    {
        return _fieldOrderCache.Get(field);
    }

    private static int CalculateAttributeOrder(FieldInfo field)
    {
        var bigEndAttr = field.CustomAttributes.OrEmpty().FirstOrDefault(a => a.AttributeType == typeof(BigEndianAttribute))?.ConstructorArguments;
        if (bigEndAttr is not null && bigEndAttr.Count == 2)
        {
            return bigEndAttr[1].Value as int? ?? throw new Exception($"Invalid {nameof(BigEndianAttribute)} definition on {field.DeclaringType?.Name}.{field.Name}");
        }

        var bigEndPartAttr = field.CustomAttributes.OrEmpty().FirstOrDefault(a => a.AttributeType == typeof(BigEndianPartialAttribute))?.ConstructorArguments;
        if (bigEndPartAttr is not null && bigEndPartAttr.Count == 2)
        {
            return bigEndPartAttr[1].Value as int? ?? throw new Exception($"Invalid {nameof(BigEndianPartialAttribute)} definition on {field.DeclaringType?.Name}.{field.Name}");
        }

        var littleEndAttr = field.CustomAttributes.OrEmpty().FirstOrDefault(a => a.AttributeType == typeof(LittleEndianAttribute))?.ConstructorArguments;
        if (littleEndAttr is not null && littleEndAttr.Count == 2)
        {
            return littleEndAttr[1].Value as int? ?? throw new Exception($"Invalid {nameof(LittleEndianAttribute)} definition on {field.DeclaringType?.Name}.{field.Name}");
        }

        var byteStrAttr = field.CustomAttributes.OrEmpty().FirstOrDefault(a => a.AttributeType == typeof(ByteStringAttribute))?.ConstructorArguments;
        if (byteStrAttr is not null && byteStrAttr.Count == 2)
        {
            return byteStrAttr[1].Value as int? ?? throw new Exception($"Invalid {nameof(ByteStringAttribute)} definition on {field.DeclaringType?.Name}.{field.Name}");
        }
        
        var asciiStrAttr = field.CustomAttributes.OrEmpty().FirstOrDefault(a => a.AttributeType == typeof(AsciiStringAttribute))?.ConstructorArguments;
        if (asciiStrAttr is not null && asciiStrAttr.Count == 2)
        {
            return asciiStrAttr[1].Value as int? ?? throw new Exception($"Invalid {nameof(AsciiStringAttribute)} definition on {field.DeclaringType?.Name}.{field.Name}");
        }

        var varByteStrAttr = field.CustomAttributes.OrEmpty().FirstOrDefault(a => a.AttributeType == typeof(VariableByteStringAttribute))?.ConstructorArguments;
        if (varByteStrAttr is not null && varByteStrAttr.Count == 2)
        {
            return varByteStrAttr[1].Value as int? ?? throw new Exception($"Invalid {nameof(VariableByteStringAttribute)} definition on {field.DeclaringType?.Name}.{field.Name}");
        }

        var vtByteStrAttr = field.CustomAttributes.OrEmpty().FirstOrDefault(a => a.AttributeType == typeof(ValueTerminatedByteStringAttribute))?.ConstructorArguments;
        if (vtByteStrAttr is not null && vtByteStrAttr.Count == 2)
        {
            return vtByteStrAttr[1].Value as int? ?? throw new Exception($"Invalid {nameof(ValueTerminatedByteStringAttribute)} definition on {field.DeclaringType?.Name}.{field.Name}");
        }
            
        var remByteAttr = field.CustomAttributes.OrEmpty().FirstOrDefault(a => a.AttributeType == typeof(RemainingBytesAttribute))?.ConstructorArguments;
        if (remByteAttr is not null && remByteAttr.Count == 1)
        {
            return remByteAttr[0].Value as int? ?? throw new Exception($"Invalid {nameof(RemainingBytesAttribute)} definition on {field.DeclaringType?.Name}.{field.Name}");
        }

        var childAttr = field.CustomAttributes.OrEmpty().FirstOrDefault(a => a.AttributeType == typeof(ByteLayoutChildAttribute))?.ConstructorArguments;
        if (childAttr is not null && childAttr.Count == 1)
        {
            return childAttr[0].Value as int? ?? throw new Exception($"Invalid {nameof(ByteLayoutChildAttribute)} definition on {field.DeclaringType?.Name}.{field.Name}");
        }

        var multiChildAttr = field.CustomAttributes.OrEmpty().FirstOrDefault(a => a.AttributeType == typeof(ByteLayoutMultiChildAttribute))?.ConstructorArguments;
        if (multiChildAttr is not null && multiChildAttr.Count == 2)
        {
            return multiChildAttr[1].Value as int? ?? throw new Exception($"Invalid {nameof(ByteLayoutMultiChildAttribute)} definition on {field.DeclaringType?.Name}.{field.Name}");
        }
        
        var varyChildAttr = field.CustomAttributes.OrEmpty().FirstOrDefault(a => a.AttributeType == typeof(ByteLayoutVariableChildAttribute))?.ConstructorArguments;
        if (varyChildAttr is not null && varyChildAttr.Count == 2)
        {
            return varyChildAttr[1].Value as int? ?? throw new Exception($"Invalid {nameof(ByteLayoutVariableChildAttribute)} definition on {field.DeclaringType?.Name}.{field.Name}");
        }

        throw new Exception($"No byte layout definition found on {field.DeclaringType?.Name}.{field.Name}");
    }


}
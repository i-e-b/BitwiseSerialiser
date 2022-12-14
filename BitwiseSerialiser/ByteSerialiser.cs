using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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
        var ok = FromBytes(typeof(T), source, out var obj);
        result = (T)obj;
        return ok;
    }


    /// <summary>
    /// Deserialise a byte array into a [ByteLayout] object.
    /// If the byte array is too short to fill the object, a partially complete object is returned.
    /// </summary>
    /// <param name="type">Target type. Must be marked with the [ByteLayout] attribute, and obey the rules of the attribute</param>
    /// <param name="source">Byte array to be deserialised</param>
    /// <param name="result">New instance of T</param>
    /// <returns>True if source was long enough to complete the result, false if too short. Returns true if source is longer than needed.</returns>
    public static bool FromBytes(Type type, IEnumerable<byte> source, out object result)
    {
        // Plan:
        // 1. get all BigEndianAttribute fields recursively, ordered appropriately
        // 2. run through each field and pull a value (increment bytes with shift?)
        // 3. return the result type
        result = Activator.CreateInstance(type) ?? throw new Exception($"Failed to create instance of {type.Name}");
        var feed = new RunOutByteQueue(source);

        RestoreObjectRecursive(feed, result);

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

        if (IsBigEnd(field, out var byteCount))
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

        if (IsByteString(field, out byteCount)) // if value is longer than declared, we truncate
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
            
        if (IsVariableByteString(field, out _)) // We don't use the calc func during serialisation, just write all bytes
        {
            var byteValues = GetValueAsByteArray(source, field);
            for (var i = 0; i < byteValues.Length; i++)
            {
                output.Add(byteValues[i]);
            }

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

        // otherwise we need to recurse deeper
        var child = field.GetValue(source);
        SerialiseObjectRecursive(child, output);
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

    private static void RestoreObjectRecursive(RunOutByteQueue feed, object output)
    {
        var publicFields = _publicFieldCache.Get(output.GetType());

        foreach (var field in publicFields)
        {
            RestoreFieldRecursive(feed, field, output);
        }
    }

    private static void RestoreFieldRecursive(RunOutByteQueue feed, FieldInfo field, object output)
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
            
        if (IsVariableByteString(field, out var functionName))
        {
            // Try to find public instance method by name, and check it's valid
            var method = field.DeclaringType?.GetMethod(functionName, BindingFlags.Public | BindingFlags.Instance);
            if (method is null) throw new Exception($"No such calculation function '{functionName}' in type {field.DeclaringType?.Name}, as declared by its field {field.Name}");
            var methodParams = method.GetParameters();
            if (methodParams.Length > 0) throw new Exception($"Invalid calculator function: {field.DeclaringType?.Name}.{functionName}({string.Join(", ",methodParams.Select(p=>p.Name))}); Calculator functions should have no parameters");
            if (method.ReturnType != typeof(int)) throw new Exception($"Calculator function {field.DeclaringType?.Name}.{functionName}() returns {method.ReturnType.Name}, but should return 'int'");

            // Call the function to get length
            byteCount = (method.Invoke(output, null!) as int?) ?? throw new Exception($"Calculator function {field.DeclaringType?.Name}.{functionName}() returned an unexpected value");
                
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

        // otherwise we need to recurse deeper
        var child = field.GetValue(output)
                    ?? Activator.CreateInstance(field.FieldType)
                    ?? throw new Exception($"Failed to find or create instance of {field.DeclaringType?.Name}.{field.Name}");

        RestoreObjectRecursive(feed, child);
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
        return Array.Empty<byte>();
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

        var varByteStrAttr = field.CustomAttributes.OrEmpty().FirstOrDefault(a => a.AttributeType == typeof(VariableByteStringAttribute))?.ConstructorArguments;
        if (varByteStrAttr is not null && varByteStrAttr.Count == 2)
        {
            return varByteStrAttr[1].Value as int? ?? throw new Exception($"Invalid {nameof(VariableByteStringAttribute)} definition on {field.DeclaringType?.Name}.{field.Name}");
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

        throw new Exception($"No byte layout definition found on {field.DeclaringType?.Name}.{field.Name}");
    }

    internal class RunOutByteQueue
    {
        private readonly Queue<byte> _q;
            
        /// <summary>
        /// Set to 'true' if more bytes were requested than supplied
        /// </summary>
        public bool WasOverRun { get; private set; }

        /// <summary> Last byte we popped when doing `NextBits` </summary>
        private byte _lastFrag;

        /// <summary> Offset in bytes (caused when reading bits). Zero means aligned </summary>
        private int _offset;

        public RunOutByteQueue(IEnumerable<byte> source)
        {
            _q = new Queue<byte>(source);
            _lastFrag = 0;
            _offset=0;
            WasOverRun = false;
        }

        /// <summary>
        /// Read a non-byte aligned number of bits.
        /// Output is in the least-significant bits.
        /// Bit count must be 1..8.
        /// <para></para>
        /// Until `NextBits` is called with a value that
        /// re-aligns the feed, `NextByte` will run slower.
        /// </summary>
        public byte NextBits(int bitCount)
        {
            if (bitCount < 1) return 0;
            if (bitCount > 8) throw new Exception("Byte queue was asked for more than one byte");

            if (_offset == 0) // we are currently aligned
            {
                if (_q.Count < 1) // there is no more data
                {
                    WasOverRun = true;
                    return 0;
                }

                _lastFrag = _q.Dequeue();
            }

            // simple case: there is enough data in the last frag
            int mask, shift;
            byte result;
            var rem = 8 - _offset;

            if (rem >= bitCount)
            {
                // example:
                // offset = 3, bit count = 3
                // 0 1 2 3 4 5 6 7
                // x x x ? ? ? _ _
                // next offset = 6
                // output = (last >> 2) & b00000111

                shift = rem - bitCount;
                mask = (1 << bitCount) - 1;
                result = (byte)((_lastFrag >> shift) & mask);
                _offset = (_offset + bitCount) % 8;
                return result;
            }

            // complex case: we need to mix data from two bytes

            // example:
            // offset = 3, bitCount = 7 => rem = 5, bitsFromNext = 2
            //
            // Input state:
            // 0 1 2 3 4 5 6 7 | 0 1 2 3 4 5 6 7
            // x x x A B C D E | F G _ _ _ _ _ _
            //
            // Output state:
            //     0 1 2 3 4 5 6 7
            // --> _ A B C D E F G 
            //
            // next offset = 2
            // output = ((last & b0001_1111) << 2) | ((next >> 6))
            if (_q.Count < 1) WasOverRun = true;
            var next = (_q.Count > 0) ? _q.Dequeue() : (byte)0;

            mask = (1 << rem) - 1;
            var bitsFromNext = bitCount - rem;
            shift = 8 - bitsFromNext;

            result = (byte)(((_lastFrag & mask) << bitsFromNext) | (next >> shift));

            _lastFrag = next;
            _offset = bitsFromNext;
            return result;
        }

        /// <summary>
        /// Remove a byte from the queue. If there are no bytes
        /// available, a zero value is returned.
        /// </summary>
        public byte NextByte()
        {
            if (_offset != 0) return NextBits(8);
            if (_q.Count > 0) return _q.Dequeue();

            // queue was empty
            WasOverRun = true;
            return 0;
        }

        public int GetRemainingLength() => _q.Count;
    }
}
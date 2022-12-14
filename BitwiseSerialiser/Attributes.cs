using System;
using System.Collections.Generic;
using System.Linq;

namespace BitwiseSerialiser;
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedType.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable BuiltInTypeReferenceStyle
// ReSharper disable MemberCanBePrivate.Global

/// <summary>
/// Silences Rider/Resharper warnings about unused fields
/// </summary>
[AttributeUsage(AttributeTargets.All)] public class MeansImplicitUseAttribute : Attribute { }

/// <summary>
/// Marks a class as representing the fields in a byte array.
/// Each FIELD in the marked class will need to have a byte order and position marker,
/// either <see cref="BigEndianAttribute"/> or <see cref="ByteLayoutChildAttribute"/>
/// </summary>
[MeansImplicitUse, AttributeUsage(AttributeTargets.Class)]
public class ByteLayoutAttribute : Attribute
{
}

/// <summary>
/// Marks a field as representing a subset of fields in a byte array.
/// The child value should be marked with <see cref="ByteLayoutAttribute"/>
/// </summary>
[MeansImplicitUse, AttributeUsage(AttributeTargets.Field)]
public class ByteLayoutChildAttribute : Attribute
{
    /// <summary>
    /// Position in bitstream relative to other fields in the container.
    /// This should start at zero and increment by 1 for each field.
    /// This is NOT the bit or byte offset.
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Represents a subset of byte field data.
    /// </summary>
    /// <param name="order">The order through the byte array in which this subset should be processed</param>
    public ByteLayoutChildAttribute(int order)
    {
        Order = order;
    }
}

/// <summary>
/// Indicates that a byte value field should always have a fixed value
/// </summary>
[MeansImplicitUse, AttributeUsage(AttributeTargets.Field)]
public class FixedValueAttribute : Attribute
{
    /// <summary>
    /// Expected value
    /// </summary>
    public byte[] Value { get; set; }

    /// <summary>
    /// Indicates that a byte value field should always have a fixed value
    /// </summary>
    public FixedValueAttribute(params byte[] value)
    {
        Value = value;
    }
}

/// <summary>
/// Represents an unsigned integer value, taking the
/// given number of bytes (1..8), MSB first.
/// Can handle non standard byte counts (e.g. 3 bytes into a UInt32)
/// </summary>
[MeansImplicitUse, AttributeUsage(AttributeTargets.Field)]
public class BigEndianAttribute : Attribute
{
    /// <summary>
    /// Byte size of the field.
    /// Number of bytes that are used for this field.
    /// </summary>
    public int Bytes { get; set; }
    
    /// <summary>
    /// Position in bitstream relative to other fields in the container.
    /// This should start at zero and increment by 1 for each field.
    /// This is NOT the bit or byte offset.
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Represents an unsigned integer value, taking the
    /// given number of bytes (1..8), MSB first.
    /// Can handle non standard byte counts (e.g. 3 bytes into a UInt32)
    /// </summary>
    /// <param name="bytes">Number of bytes in this value. Can be any number from 1 to 8 inclusive.</param>
    /// <param name="order">The order through the byte array in which this value should be processed</param>
    public BigEndianAttribute(int bytes, int order)
    {
        Bytes = bytes;
        Order = order;
    }

    /// <summary>
    /// List of field types that BigEndian can be validly applied to
    /// </summary>
    public static readonly Type[] AcceptableTypes =
    {
        typeof(byte), typeof(UInt16), typeof(UInt32), typeof(UInt64),
        typeof(Int16), typeof(Int32), typeof(Int64)
    };

    /// <summary>
    /// Returns true if the given type can be used for BigEndian fields
    /// </summary>
    public static bool IsAcceptable(Type? fieldType)
    {
        return AcceptableTypes.Contains(fieldType);
    }
}

/// <summary>
/// Represents an unsigned integer value, taking the
/// given number of BITS (1..64), MSB first.
/// Can handle non standard bit counts (e.g. 13 bits into a UInt16)
/// <para></para>
/// A sequence of BigEndianPartial attributes should line up to a byte boundary.
/// </summary>
[MeansImplicitUse, AttributeUsage(AttributeTargets.Field)]
public class BigEndianPartialAttribute : Attribute
{
    /// <summary>
    /// BIT size of the field.
    /// Number of bit that are used for this field.
    /// </summary>
    public int Bits { get; set; }
    
    /// <summary>
    /// Position in bitstream relative to other fields in the container.
    /// This should start at zero and increment by 1 for each field.
    /// This is NOT the bit or byte offset.
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Represents an unsigned integer value, taking the
    /// given number of bits (1..64), MSB first.
    /// Can handle non standard bit counts (e.g. 13 bits into a UInt16)
    /// </summary>
    /// <param name="bits">Number of bits in this value. Can be any number from 1 to 64 inclusive.</param>
    /// <param name="order">The order through the byte array in which this value should be processed</param>
    public BigEndianPartialAttribute(int bits, int order)
    {
        Bits = bits;
        Order = order;
    }

    /// <summary>
    /// List of field types that BigEndianPartial can be validly applied to
    /// </summary>
    public static readonly Type[] AcceptableTypes =
    {
        typeof(byte), typeof(UInt16), typeof(UInt32), typeof(UInt64),
        typeof(Int16), typeof(Int32), typeof(Int64)
    };

    /// <summary>
    /// Returns true if the given type can be used for BigEndian fields
    /// </summary>
    public static bool IsAcceptable(Type? fieldType)
    {
        return AcceptableTypes.Contains(fieldType);
    }
}

/// <summary>
/// Represents an unsigned integer value, taking the
/// given number of bytes (1..8), LSB first.
/// Can handle non standard byte counts (e.g. 3 bytes into a UInt32)
/// </summary>
[MeansImplicitUse, AttributeUsage(AttributeTargets.Field)]
public class LittleEndianAttribute : Attribute
{
    /// <summary>
    /// Byte size of the field.
    /// Number of bytes that are used for this field.
    /// </summary>
    public int Bytes { get; set; }
    
    /// <summary>
    /// Position in bitstream relative to other fields in the container.
    /// This should start at zero and increment by 1 for each field.
    /// This is NOT the bit or byte offset.
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Represents an unsigned integer value, taking the
    /// given number of bytes (1..8), LSB first.
    /// Can handle non standard byte counts (e.g. 3 bytes into a UInt32)
    /// </summary>
    /// <param name="bytes">Number of bytes in this value. Can be any number from 1 to 8 inclusive.</param>
    /// <param name="order">The order through the byte array in which this value should be processed</param>
    public LittleEndianAttribute(int bytes, int order)
    {
        Bytes = bytes;
        Order = order;
    }

    /// <summary>
    /// List of field types that BigEndian can be validly applied to
    /// </summary>
    public static readonly Type[] AcceptableTypes =
    {
        typeof(byte), typeof(UInt16), typeof(UInt32), typeof(UInt64),
        typeof(Int16), typeof(Int32), typeof(Int64)
    };

    /// <summary>
    /// Returns true if the given type can be used for BigEndian fields
    /// </summary>
    public static bool IsAcceptable(Type? fieldType)
    {
        return AcceptableTypes.Contains(fieldType);
    }
}

/// <summary>
/// Represents a known-length list of bytes in input order
/// </summary>
[MeansImplicitUse, AttributeUsage(AttributeTargets.Field)]
public class ByteStringAttribute : Attribute
{
    /// <summary>
    /// Byte size of the field.
    /// Number of bytes that are used for this field.
    /// </summary>
    public int Bytes { get; set; }
    
    /// <summary>
    /// Position in bitstream relative to other fields in the container.
    /// This should start at zero and increment by 1 for each field.
    /// This is NOT the bit or byte offset.
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Represents a known-length list of bytes in input order
    /// </summary>
    /// <param name="bytes">Number of bytes in this value</param>
    /// <param name="order">The order through the byte array in which this value should be processed</param>
    public ByteStringAttribute(int bytes, int order)
    {
        Bytes = bytes;
        Order = order;
    }

    /// <summary>
    /// List of field types that BigEndian can be validly applied to
    /// </summary>
    public static readonly Type[] AcceptableTypes = { typeof(byte[]) };

    /// <summary>
    /// Returns true if the given type can be used for BigEndian fields
    /// </summary>
    public static bool IsAcceptable(Type? fieldType)
    {
        return AcceptableTypes.Contains(fieldType);
    }
}

/// <summary>
/// Represents an unknown length list of bytes in input order,
/// from the current position to the end of input.
/// <para></para>
/// This should be the last field by order.
/// During serialisation, this is treated as a normal byte string.
/// </summary>
[MeansImplicitUse, AttributeUsage(AttributeTargets.Field)]
public class RemainingBytesAttribute : Attribute
{
    /// <summary>
    /// Position in bitstream relative to other fields in the container.
    /// This should start at zero and increment by 1 for each field.
    /// This is NOT the bit or byte offset.
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Represents a list of bytes in input order, from current position to end of input.
    /// </summary>
    /// <param name="order">The order through the byte array in which this value should be processed. This should be the last field by order</param>
    public RemainingBytesAttribute(int order)
    {
        Order = order;
    }

    /// <summary>
    /// List of field types that BigEndian can be validly applied to
    /// </summary>
    public static readonly Type[] AcceptableTypes = { typeof(byte[]) };

    /// <summary>
    /// Returns true if the given type can be used for BigEndian fields
    /// </summary>
    public static bool IsAcceptable(Type? fieldType)
    {
        return AcceptableTypes.Contains(fieldType);
    }
}

/// <summary>
/// Represents a list of bytes in input order, whose length is
/// generated by a named function of the type.
/// <para></para>
/// The function should be a public instance method that takes
/// no parameters and returns an int.
/// The function is allowed to return zero or negative values,
/// which will be interpreted as empty. The resulting byte array
/// will be non-null and zero length.
/// <para></para>
/// When based on another field, that field MUST be in earlier order than
/// the variable byte string.
/// </summary>
[MeansImplicitUse, AttributeUsage(AttributeTargets.Field)]
public class VariableByteStringAttribute : Attribute
{
    /// <summary>
    /// Name of a method on this container class
    /// that will give the size of this byte string.
    /// </summary>
    public string Source { get; set; }
    
    /// <summary>
    /// Position in bitstream relative to other fields in the container.
    /// This should start at zero and increment by 1 for each field.
    /// This is NOT the bit or byte offset.
    /// </summary>
    public int Order { get; set; }
        
    /// <summary>
    /// Set attribute with source and order
    /// </summary>
    /// <param name="source">Name of a method on this container class
    /// that will give the size of this byte string.</param>
    /// <param name="order">Position in bitstream relative to other fields in the container.
    /// This should start at zero and increment by 1 for each field.
    /// This is NOT the bit or byte offset.</param>
    public VariableByteStringAttribute(string source, int order)
    {
        Source = source;
        Order = order;
    }

    /// <summary>
    /// List of field types that BigEndian can be validly applied to
    /// </summary>
    public static readonly Type[] AcceptableTypes = { typeof(byte[]) };

    /// <summary>
    /// Returns true if the given type can be used for BigEndian fields
    /// </summary>
    public static bool IsAcceptable(Type? fieldType)
    {
        return AcceptableTypes.Contains(fieldType);
    }
}

internal class ByteWriter
{
    private readonly List<byte> _output;
    private int _offset;
    private byte _waiting;

    public ByteWriter()
    {
        _offset = 0;
        _waiting = 0;
        _output = new List<byte>();
    }
        
    public byte[] ToArray()
    {
        // flush last byte?
        if (_offset != 0) _output.Add(_waiting);
            
        // return output
        return _output.ToArray();
    }
        
    public void Add(byte value)
    {
        if (_offset == 0) _output.Add(value);
        else WriteBitsBigEndian(value, 8);
    }

    public void WriteBytesBigEnd(ulong value, int byteCount)
    {
        for (var i = byteCount - 1; i >= 0; i--)
        {
            Add((byte)((value >> (i * 8)) & 0xFF));
        }
    }

    public void WriteBytesLittleEnd(ulong value, int byteCount)
    {
        for (var i = 0; i < byteCount; i++)
        {
            Add((byte)((value >> (i * 8)) & 0xFF));
        }
    }


    /// <summary>
    /// Write a partial byte. While input is not byte aligned,
    /// all writes will be slower
    /// </summary>
    public void WriteBitsBigEndian(ulong value, int bitCount)
    {
        if (bitCount < 1) return;
        if (bitCount > 64) throw new Exception("Invalid bit length in ByteWriter");
            
        var rem = 8 - _offset;
        int shift;
        ulong mask;
            
        // Simple case: will it fit in remaining data of one byte?
        if (bitCount <= rem)
        {
            // example:
            // offset = 3, bit count = 3; value = _ ... _ A B C
            // 0 1 2 3 4 5 6 7
            // x x x A B C _ _
            // next offset = 6
            // _waiting |= (value & b00000111) << 2
                
            shift = rem - bitCount;
            mask = (1ul << bitCount) - 1ul;
            _waiting |= (byte)((value & mask) << shift);
            _offset = (_offset + bitCount) % 8;
            if (_offset == 0)
            {
                _output.Add(_waiting);
                _waiting = 0;
            }
            return;
        }
            
        // Complex case: Data spans multiple bytes
            
        // example 1:
        // offset = 3 (rem = 5), bitCount = 16
        //
        // /-- _waiting --\
        // 0 1 2 3 4 5 6 7 | 0 1 2 3 4 5 6 7 | 0 1 2 3 4 5 6 7
        // x x x A B C D E | F G H I J K L M | N O P _ _ _ _ _
        //
        // bitCount 
        // byte[+0] = ((value & b1111_1xXx__xXxX_xXxX) >> 11)
        // byte[+1] = ((value & b0000_0111__1111_1xXx) >>  3)
        // byte[+2] = ((value & b0000_0000__0000_0111) <<  5)
            
        // example 2:
        // offset = 3 (rem = 5), bitCount = 13
        //
        // /-- _waiting --\
        // 0 1 2 3 4 5 6 7 | 0 1 2 3 4 5 6 7
        // x x x A B C D E | F G H I J K L M
        //
        // bitCount 
        // byte[+0] = ((value & b0001_1111__xXxX_xXxX) >> 8)
        // byte[+1] = ((value & b0000_0000__1111_1111) >>  3)
            
        // first partial
        shift = bitCount - rem;
        mask = (1ul << bitCount) - 1;
        _waiting |= (byte)((value & mask) >> shift);
        _output.Add(_waiting);
        _waiting = 0;
        _offset = 0;
            
        // as many completes as will fit
        while (shift > 0){
            shift -= 8;
            _output.Add((byte)((value >> shift) & 0xff));
        }
            
        // last partial, only if we're going to end un-aligned
        if (shift < 0)
        {
            shift = -shift;
            _offset = 8 - shift;
            _waiting = (byte)((value & 0xff) << shift);
        }
    }
}
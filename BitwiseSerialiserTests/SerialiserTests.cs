using System.Diagnostics.CodeAnalysis;
using System.Text;
using BitwiseSerialiser;
using NUnit.Framework;
#pragma warning disable CS8602

namespace BitwiseSerialiserTests;

[TestFixture]
public class SerialiserTests
{
    [Test]
    public void round_trip_of_simple_structure()
    {
        //                        [ fixed ]  [ big endian ]    [ little end ]   [ fixed ]
        var expected = new byte[]{0x7F,0x80, 0x12,0x34,0x56,   0x67,0x45,0x23,  0x55,0xAA};
        
        var src = new SimpleByteStructure{
            ThreeBytesBig = 0x123456,
            ThreeBytesSmall = 0x234567
        };
        
        var actual = ByteSerialiser.ToBytes(src);
        
        Console.WriteLine(Convert.ToHexString(actual));
        Assert.That(actual, Is.EqualTo(expected).AsCollection, "serialised value");
        
        var ok = ByteSerialiser.FromBytes<SimpleByteStructure>(actual, out var dst);
        
        Assert.That(ok, Is.True, "validation");
        Assert.That(dst, Is.Not.Null, "result object");
        Assert.That(dst.ThreeBytesBig, Is.EqualTo(0x123456), "result object");
        Assert.That(dst.ThreeBytesSmall, Is.EqualTo(0x234567), "result object");
    }
    
    [Test]
    public void incoming_fields_with_fixed_values_are_exposed_as_the_real_incoming_value()
    {
        //                   [ wrong ]                               [ wrong ]
        var src = new byte[]{0xAB,0xCD,0x12,0x34,0x56,0x67,0x45,0x23,0xBC,0xDE};
        
        var ok = ByteSerialiser.FromBytes<SimpleByteStructure>(src, out var dst);
        
        Assert.That(ok, Is.EqualTo(true)); // we don't validate on fixed
        
        Assert.That(dst.StartMarker, Is.EqualTo(0xABCD));
        Assert.That(dst.EndMarker, Is.EqualTo(0xDEBC)); // we respect byte ordering on read
    }

    [Test]
    public void can_get_a_debug_description_of_byte_strings()
    {
        var expected = @"StartMarker: 0x7F80 (32640)
ThreeBytesBig: 0x00123456 (1193046)
ThreeBytesSmall: 0x00234567 (2311527)
EndMarker: 0x55AA (21930)
";
        var src = new byte[]{0x7F,0x80, 0x12,0x34,0x56,   0x67,0x45,0x23,  0xAA,0x55};
        
        ByteSerialiser.FromBytes<SimpleByteStructure>(src, out var dst);
        var desc = TypeDescriber.Describe(dst);
        
        Console.WriteLine(desc);
        Assert.That(FixNewLines(desc), Is.EqualTo(FixNewLines(expected)));
    }

    [Test]
    public void can_read_out_fractional_bytes_in_big_endian_mode()
    {
        var expected = new byte[] { 0x49 };
        
        var src = new SimpleBitwiseStructure{
            FirstThreeBits = 2,
            MiddleTwoBits = 1,
            LastThreeBits = 1
        };
        
        var actual = ByteSerialiser.ToBytes(src);
        
        Console.WriteLine(Convert.ToHexString(actual));
        Assert.That(actual, Is.EqualTo(expected).AsCollection, "serialised value");
        
        var ok = ByteSerialiser.FromBytes<SimpleBitwiseStructure>(actual, out var dst);
        
        Assert.That(ok, Is.True);
        Assert.That(dst.FirstThreeBits, Is.EqualTo(src.FirstThreeBits), "1");
        Assert.That(dst.MiddleTwoBits, Is.EqualTo(src.MiddleTwoBits), "2");
        Assert.That(dst.LastThreeBits, Is.EqualTo(src.LastThreeBits), "3");
    }
    
    [Test]
    public void byte_layout_elements_can_be_nested_inside_others()
    {
        var expected = new byte[] { 0x7F, 0x80, 0x49 };
        
        var src = new ParentByteStructure{
            ChildStruct = new SimpleBitwiseStructure
            {
                FirstThreeBits = 2,
                MiddleTwoBits = 1,
                LastThreeBits = 1
            }
        };
        
        var actual = ByteSerialiser.ToBytes(src);
        
        Console.WriteLine(Convert.ToHexString(actual));
        Assert.That(actual, Is.EqualTo(expected).AsCollection, "serialised value");
        
        var ok = ByteSerialiser.FromBytes<ParentByteStructure>(actual, out var dst);
        
        Assert.That(ok, Is.True);
        Assert.That(dst.StartMarker[0], Is.EqualTo(0x7F), "1");
        Assert.That(dst.StartMarker[1], Is.EqualTo(0x80), "2");
        Assert.That(dst.ChildStruct.FirstThreeBits, Is.EqualTo(src.ChildStruct.FirstThreeBits), "3");
        Assert.That(dst.ChildStruct.MiddleTwoBits, Is.EqualTo(src.ChildStruct.MiddleTwoBits), "4");
        Assert.That(dst.ChildStruct.LastThreeBits, Is.EqualTo(src.ChildStruct.LastThreeBits), "5");
    }
    
    [Test]
    public void can_repeat_byte_layouts_nested_inside_parents()
    {
        var expected = new byte[] { 0x55, 0x49, 0xFF, 0x48, 0xAA };
        
        var src = new ParentWithRepeatedChild{
            ChildStruct = new SimpleBitwiseStructure[]
            {
                new()
                {
                    FirstThreeBits = 2,
                    MiddleTwoBits = 1,
                    LastThreeBits = 1
                },
                new()
                {
                FirstThreeBits = 7,
                MiddleTwoBits = 3,
                LastThreeBits = 7
                },
                new()
                {
                    FirstThreeBits = 2,
                    MiddleTwoBits = 1,
                    LastThreeBits = 0
                }
            }
        };
        
        var actual = ByteSerialiser.ToBytes(src);
        
        Console.WriteLine(Convert.ToHexString(actual));
        Assert.That(actual, Is.EqualTo(expected).AsCollection, "serialised value");
        
        var ok = ByteSerialiser.FromBytes<ParentWithRepeatedChild>(actual, out var dst);
        
        Assert.That(ok, Is.True);
        Assert.That(dst.StartMarker[0], Is.EqualTo(0x55), "1");
        Assert.That(dst.EndMarker[0], Is.EqualTo(0xAA), "2");
        
        Assert.That(dst.ChildStruct[0].FirstThreeBits, Is.EqualTo(src.ChildStruct[0].FirstThreeBits), "3a");
        Assert.That(dst.ChildStruct[0].MiddleTwoBits, Is.EqualTo(src.ChildStruct[0].MiddleTwoBits), "4a");
        Assert.That(dst.ChildStruct[0].LastThreeBits, Is.EqualTo(src.ChildStruct[0].LastThreeBits), "5a");
        
        Assert.That(dst.ChildStruct[1].FirstThreeBits, Is.EqualTo(src.ChildStruct[1].FirstThreeBits), "3b");
        Assert.That(dst.ChildStruct[1].MiddleTwoBits, Is.EqualTo(src.ChildStruct[1].MiddleTwoBits), "4b");
        Assert.That(dst.ChildStruct[1].LastThreeBits, Is.EqualTo(src.ChildStruct[1].LastThreeBits), "5b");
        
        Assert.That(dst.ChildStruct[2].FirstThreeBits, Is.EqualTo(src.ChildStruct[2].FirstThreeBits), "3c");
        Assert.That(dst.ChildStruct[2].MiddleTwoBits, Is.EqualTo(src.ChildStruct[2].MiddleTwoBits), "4c");
        Assert.That(dst.ChildStruct[2].LastThreeBits, Is.EqualTo(src.ChildStruct[2].LastThreeBits), "5c");
    }
    
    [Test]
    public void can_have_repeat_byte_layouts_with_variable_repeat_count_nested_inside_parents()
    {
        var expected = new byte[] { 0x55, 0x00, 0x03, 0x7F, 0x80, 0x00, 0x01, 0xC8, 
            0x7B, 0x00, 0x00, 0x55, 0xAA, 0x7F, 0x80, 0x00, 0x00, 0x7B, 0x15, 0x03,
            0x00, 0x55, 0xAA, 0x7F, 0x80, 0x00, 0x03, 0x15, 0xC8, 0x01, 0x00, 0x55, 0xAA, 0xAA
        };
        
        var src = new ParentWithVariableRepeatChild{
            HowMany = 3,
            ChildStruct = new SimpleByteStructure[]
            {
                new()
                {
                    ThreeBytesSmall = 123,
                    ThreeBytesBig = 456
                },
                new()
                {
                    ThreeBytesSmall = 789,
                    ThreeBytesBig = 123
                },
                new()
                {
                    ThreeBytesSmall = 456,
                    ThreeBytesBig = 789
                }
            }
        };
        
        var actual = ByteSerialiser.ToBytes(src);
        
        Console.WriteLine(Convert.ToHexString(actual));
        Assert.That(actual, Is.EqualTo(expected).AsCollection, "serialised value");
        
        var ok = ByteSerialiser.FromBytes<ParentWithVariableRepeatChild>(actual, out var dst);
        
        Assert.That(ok, Is.True);
        Assert.That(dst.StartMarker[0], Is.EqualTo(0x55), "0");
        Assert.That(dst.EndMarker[0], Is.EqualTo(0xAA), "1");
        Assert.That(dst.HowMany, Is.EqualTo(3), "2");
        
        Assert.That(dst.ChildStruct[0].ThreeBytesSmall, Is.EqualTo(src.ChildStruct[0].ThreeBytesSmall), "3a");
        Assert.That(dst.ChildStruct[0].ThreeBytesBig, Is.EqualTo(src.ChildStruct[0].ThreeBytesBig), "4a");
        Assert.That(dst.ChildStruct[0].StartMarker, Is.EqualTo(0x7F80), "5a");
        Assert.That(dst.ChildStruct[0].EndMarker, Is.EqualTo(0xAA55), "6a");
        
        Assert.That(dst.ChildStruct[1].ThreeBytesSmall, Is.EqualTo(src.ChildStruct[1].ThreeBytesSmall), "3b");
        Assert.That(dst.ChildStruct[1].ThreeBytesBig, Is.EqualTo(src.ChildStruct[1].ThreeBytesBig), "4b");
        Assert.That(dst.ChildStruct[1].StartMarker, Is.EqualTo(0x7F80), "5b");
        Assert.That(dst.ChildStruct[1].EndMarker, Is.EqualTo(0xAA55), "6b");
        
        Assert.That(dst.ChildStruct[2].ThreeBytesSmall, Is.EqualTo(src.ChildStruct[2].ThreeBytesSmall), "3c");
        Assert.That(dst.ChildStruct[2].ThreeBytesBig, Is.EqualTo(src.ChildStruct[2].ThreeBytesBig), "4c");
        Assert.That(dst.ChildStruct[2].StartMarker, Is.EqualTo(0x7F80), "5c");
        Assert.That(dst.ChildStruct[2].EndMarker, Is.EqualTo(0xAA55), "6c");
        
        Console.WriteLine(TypeDescriber.Describe(dst));
    }

    [Test]
    public void can_have_a_fixed_length_byte_string()
    {
        var expected = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x10 };

        var src = new FixedArrayStructure
        {
            FixedArray = [0x01, 0x02, 0x03, 0x04, 0x05],
            NullableArray = [0x06, 0x07, 0x08, 0x09, 0x10]
        };
        
        var actual = ByteSerialiser.ToBytes(src);
        
        Console.WriteLine(Convert.ToHexString(actual));
        Assert.That(actual, Is.EqualTo(expected).AsCollection, "serialised value");
        
        var ok = ByteSerialiser.FromBytes<FixedArrayStructure>(actual, out var dst);
        Assert.That(ok, Is.True);
        
        Assert.That(dst.FixedArray, Is.EqualTo(src.FixedArray).AsCollection, "1");
        Assert.That(dst.NullableArray, Is.EqualTo(src.NullableArray).AsCollection, "2");
    }
    
    [Test]
    public void can_have_a_fixed_length_ascii_string()
    {
        var expected = new byte[]{0x48,0x65,0x6C,0x6C,0x6F,0x57,0x6F,0x72,0x6C,0x64};

        var src = new FixedArrayWithStringStructure
        {
            FixedString = "Hello",
            NullableString = "World",
        };
        
        var actual = ByteSerialiser.ToBytes(src);
        
        Console.WriteLine(Convert.ToHexString(actual));
        Assert.That(actual, Is.EqualTo(expected).AsCollection, "serialised value");
        
        var ok = ByteSerialiser.FromBytes<FixedArrayWithStringStructure>(actual, out var dst);
        Assert.That(ok, Is.True);
        
        Assert.That(dst.FixedString, Is.EqualTo(src.FixedString).AsCollection, "1");
        Assert.That(dst.NullableString, Is.EqualTo(src.NullableString).AsCollection, "2");

        Console.WriteLine(TypeDescriber.Describe(dst));
    }

    [Test]
    public void can_have_variable_length_byte_strings_based_on_other_fields()
    {
        var expected = new byte[] { 0x00, 0x05, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x10 };

        var src = new VariableArrayStructure
        {
            Length = 5,
            Variable = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 },
            NullableVariable = new byte[] { 0x06, 0x07, 0x08, 0x09, 0x10 }
        };
        
        var actual = ByteSerialiser.ToBytes(src);
        
        Console.WriteLine(Convert.ToHexString(actual));
        Assert.That(actual, Is.EqualTo(expected).AsCollection, "serialised value");
        
        var ok = ByteSerialiser.FromBytes<VariableArrayStructure>(actual, out var dst);
        Assert.That(ok, Is.True);
        
        Assert.That(dst.Length, Is.EqualTo(src.Length), "1");
        Assert.That(dst.Variable, Is.EqualTo(src.Variable).AsCollection, "2");
        Assert.That(dst.NullableVariable, Is.EqualTo(src.NullableVariable).AsCollection, "3");
    }

    [Test]
    public void can_collect_all_remaining_bytes_into_an_array()
    {
        var expected = new byte[] { 0x12, 0x34, 0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x2C, 0x20, 0x77, 0x6F, 0x72, 0x6C, 0x64, 0x21 };

        var src = new RemainingBytesStructure
        {
            SomethingElse = 0x1234,
            VariableArray = Encoding.UTF8.GetBytes("Hello, world!")
        };
        
        var actual = ByteSerialiser.ToBytes(src);
        
        Console.WriteLine(Convert.ToHexString(actual));
        Assert.That(actual, Is.EqualTo(expected).AsCollection, "serialised value");
        
        var ok = ByteSerialiser.FromBytes<RemainingBytesStructure>(actual, out var dst);
        Assert.That(ok, Is.True);
        
        Assert.That(dst.SomethingElse, Is.EqualTo(src.SomethingElse), "1");
        Assert.That(dst.VariableArray, Is.EqualTo(src.VariableArray).AsCollection, "2");
    }
    
    [Test]
    public void can_have_variable_length_byte_strings_based_on_stop_valued_bytes()
    {
        var expected = new byte[] { 0x12,0x34, 0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x2C, 0x20, 0x77, 0x6F, 0x72, 0x6C, 0x64, 0x21, 0x00, 0x56, 0x78};

        var src = new NullTerminatedStructure
        {
            VariableArray = Encoding.UTF8.GetBytes("Hello, world!\0")
        };
        
        var actual = ByteSerialiser.ToBytes(src);
        
        Console.WriteLine(Convert.ToHexString(actual));
        Assert.That(actual, Is.EqualTo(expected).AsCollection, "serialised value");
        
        var ok = ByteSerialiser.FromBytes<NullTerminatedStructure>(actual, out var dst);
        Assert.That(ok, Is.True);
        
        Assert.That(dst.Header, Is.EqualTo(0x1234), "1");
        Assert.That(dst.VariableArray, Is.EqualTo(src.VariableArray).AsCollection, "2");
        Assert.That(dst.Footer, Is.EqualTo(0x5678), "3");
    }
    
    [Test]
    public void when_stop_valued_bytes_strings_are_supplied_without_the_stop_value_then_it_is_inserted()
    {
        var expected = new byte[] { 0x12,0x34, 0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x2C, 0x20, 0x77, 0x6F, 0x72, 0x6C, 0x64, 0x21, 0x00, 0x56, 0x78};

        var src = new NullTerminatedStructure
        {
            VariableArray = Encoding.UTF8.GetBytes("Hello, world!") // No '\0' here
        };
        
        var actual = ByteSerialiser.ToBytes(src);
        
        Console.WriteLine(Convert.ToHexString(actual));
        Assert.That(actual, Is.EqualTo(expected).AsCollection, "serialised value");
        
        var ok = ByteSerialiser.FromBytes<NullTerminatedStructure>(actual, out var dst);
        Assert.That(ok, Is.True);
        
        Assert.That(dst.Header, Is.EqualTo(0x1234), "1");
        Assert.That(dst.VariableArray, Is.EqualTo(Encoding.UTF8.GetBytes("Hello, world!\0")).AsCollection, "2"); // But the '\0' is in the result
        Assert.That(dst.Footer, Is.EqualTo(0x5678), "3");
    }

    [Test]
    public void can_specialise_main_type_when_deserialising()
    {
        var src = new GenericParent
        {
            TypeNumber = 1,
            GenericData = 0x1234
        };
        
        // Without special form
        var actual = ByteSerialiser.ToBytes(src).Concat("GOOD"u8.ToArray());
        var ok = ByteSerialiser.FromBytes<GenericParent>(actual, out var dst);
        
        Assert.That(ok, Is.True, "validation");
        Assert.That(dst, Is.Not.Null, "result object");
        Assert.That(dst.TypeNumber, Is.EqualTo(1), "result object");
        Assert.That(dst.GenericData, Is.EqualTo(0x1234), "result object");
        
        // With special form
        src.TypeNumber = 3;
        actual = ByteSerialiser.ToBytes(src).Concat("GOOD"u8.ToArray());
        ok = ByteSerialiser.FromBytes<GenericParent>(actual, out dst);
        
        Assert.That(ok, Is.True, "validation");
        Assert.That(dst, Is.Not.Null, "result object");
        Assert.That(dst.GetType(), Is.EqualTo(typeof(SpecialParent)), "result object");
        Assert.That(dst.TypeNumber, Is.EqualTo(3), "result object");
        Assert.That(dst.GenericData, Is.EqualTo(4660), "result object");
        Assert.That(((SpecialParent)dst).FixedString, Is.EqualTo("GOOD"), "result object");

        Console.WriteLine(TypeDescriber.Describe(dst));
    }

    private static string FixNewLines(string result) => result.Replace("\r", "");
}

[ByteLayout(SpecialiseWith = nameof(TableSpecialise))]
public class GenericParent
{
    [BigEndian(bytes: 2, order: 0)]
    public int TypeNumber;

    [BigEndian(bytes: 2, order: 1)]
    public int GenericData;
    
    public Type? TableSpecialise()
    {
        return TypeNumber switch
        {
            3 => typeof(SpecialParent),
            _ => null
        };
    }
}

[ByteLayout]
public class SpecialParent:GenericParent
{
    [AsciiString(bytes: 4, order: 2)]
    public string FixedString = "BAD!";
}



[ByteLayout]
[SuppressMessage("ReSharper", "UnassignedField.Global")]
public class NullTerminatedStructure
{
    [BigEndian(bytes: 2, order: 0), FixedValue(0x12,0x34)]
    public int Header;
    
    [ValueTerminatedByteString(stopValue: 0x00, order: 1)]
    public byte[]? VariableArray;
    
    [BigEndian(bytes: 2, order: 2), FixedValue(0x56,0x78)]
    public int Footer;
}

[ByteLayout]
[SuppressMessage("ReSharper", "UnassignedField.Global")]
public class VariableArrayStructure
{
    [BigEndian(bytes: 2, order: 0)]
    public int Length;
    
    [VariableByteString(source: nameof(GetLength), order: 1)]
    public byte[] Variable = Array.Empty<byte>();
    
    [VariableByteString(source: nameof(GetLength), order: 1)]
    public byte[]? NullableVariable;
    
    public int GetLength()=>Length;
}

[ByteLayout]
[SuppressMessage("ReSharper", "UnassignedField.Global")]
public class RemainingBytesStructure
{
    [BigEndian(bytes: 2, order: 0)]
    public int SomethingElse;
    
    [RemainingBytes(order: 1)]
    public byte[]? VariableArray;
}

[ByteLayout]
[SuppressMessage("ReSharper", "UnassignedField.Global")]
public class FixedArrayStructure
{
    [ByteString(bytes: 5, order: 0)]
    public byte[] FixedArray = [];
    
    [ByteString(bytes: 5, order: 1)]
    public byte[]? NullableArray;
}

[ByteLayout]
[SuppressMessage("ReSharper", "UnassignedField.Global")]
public class FixedArrayWithStringStructure
{
    [AsciiString(bytes: 5, order: 0)]
    public string FixedString = "";
    
    [AsciiString(bytes: 5, order: 1)]
    public string? NullableString;
}

[ByteLayout]
[SuppressMessage("ReSharper", "UnassignedField.Global")]
public class SimpleByteStructure
{
    [BigEndian(bytes: 2, order: 0)] [FixedValue(0x7F, 0x80)]
    public UInt16 StartMarker;
    
    [BigEndian(bytes: 3, order: 1)]
    public UInt32 ThreeBytesBig;
    
    [LittleEndian(bytes: 3, order: 2)]
    public UInt32 ThreeBytesSmall;
    
    [LittleEndian(bytes: 2, order: 3)] [FixedValue(0xAA, 0x55)]
    public UInt16 EndMarker;
}

[ByteLayout]
[SuppressMessage("ReSharper", "UnassignedField.Global")]
public class SimpleBitwiseStructure
{
    [BigEndianPartial(bits:3, order: 0)]
    public byte FirstThreeBits;
    
    [BigEndianPartial(bits:2, order: 1)]
    public byte MiddleTwoBits;
    
    [BigEndianPartial(bits:3, order: 1)]
    public byte LastThreeBits;
}

[ByteLayout]
[SuppressMessage("ReSharper", "UnassignedField.Global")]
public class ParentByteStructure
{
    [ByteString(bytes: 2, order: 0)] [FixedValue(0x7F, 0x80)]
    public byte[]? StartMarker;
    
    [ByteLayoutChild(order: 1)]
    public SimpleBitwiseStructure? ChildStruct;
}

[ByteLayout]
[SuppressMessage("ReSharper", "UnassignedField.Global")]
public class ParentWithRepeatedChild
{
    [ByteString(bytes: 1, order: 0)] [FixedValue(0x55)]
    public byte[]? StartMarker;
    
    [ByteLayoutMultiChild(count: 3, order: 1)]
    public SimpleBitwiseStructure[]? ChildStruct;
    
    [ByteString(bytes: 1, order: 2)] [FixedValue(0xAA)]
    public byte[]? EndMarker;
}

[ByteLayout]
[SuppressMessage("ReSharper", "UnassignedField.Global")]
public class ParentWithVariableRepeatChild
{
    [ByteString(bytes: 1, order: 0)] [FixedValue(0x55)]
    public byte[]? StartMarker;
    
    [BigEndian(bytes: 2, order:1)]
    public int HowMany;
    
    [ByteLayoutVariableChild(nameof(CountHowMany), order: 2)]
    public SimpleByteStructure[]? ChildStruct;
    
    [ByteString(bytes: 1, order: 3)] [FixedValue(0xAA)]
    public byte[]? EndMarker;

    public int CountHowMany() => HowMany;
}
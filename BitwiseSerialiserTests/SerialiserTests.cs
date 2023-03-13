using System.Diagnostics.CodeAnalysis;
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
    }

    private static string FixNewLines(string result) => result.Replace("\r", "");
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
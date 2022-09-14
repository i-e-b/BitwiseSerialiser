using BitwiseSerialiser;
using NUnit.Framework;

namespace BitwiseSerialiserTests;

[TestFixture]
public class ExtensionTests
{
    [Test]
    public void bcd_conversion()
    {
        Assert.That(34.DecToBcd(), Is.EqualTo(0x34));
        
        var ok = ((byte)0x34).BcdToDec(out var dec);
        Assert.That(ok, Is.True);
        Assert.That(dec, Is.EqualTo(34));
        
        ok = ((byte)0xAC).BcdToDec(out dec);
        Assert.That(ok, Is.False);
        Assert.That(dec, Is.EqualTo(112));
    }

    [Test]
    [TestCase(1ul, "1b")]
    [TestCase(1000ul, "1000b")]
    [TestCase(2048ul, "2kb")]
    [TestCase(1000000ul, "976.56kb")]
    [TestCase(1048576ul, "1mb")]
    [TestCase(1000000000ul, "953.67mb")]
    [TestCase(1073741824ul, "1gb")]
    [TestCase(1099511627776ul, "1tb")]
    [TestCase(1125899906842624ul, "1pb")]
    public void human_readable_sizes(ulong size, string expected)
    {
        Assert.That(size.Human(), Is.EqualTo(expected), "ulong");

        if (size <= long.MaxValue)
            Assert.That(((long)size).Human(), Is.EqualTo(expected), "long");
        
        if (size <= uint.MaxValue)
            Assert.That(((uint)size).Human(), Is.EqualTo(expected), "uint");
        
        if (size <= int.MaxValue)
            Assert.That(((int)size).Human(), Is.EqualTo(expected), "int");
        
        if (size <= ushort.MaxValue)
            Assert.That(((ushort)size).Human(), Is.EqualTo(expected), "ushort");
        
        if (size <= (int)short.MaxValue)
            Assert.That(((short)size).Human(), Is.EqualTo(expected), "short");
    }

    [Test]
    public void output_bytes_to_cSharp_code()
    {
        var sample = new byte[] { 1, 2, 3, 100, 200, 255, 0 };
        
        var fullOutput = sample.ToCsharpCode("varName");
        Console.WriteLine(fullOutput);
        Assert.That(fullOutput, Is.EqualTo(
            "var varName = new byte[] {0x01, 0x02, 0x03, 0x64, 0xC8, 0xFF, 0x00};"), "full");
        
        var subset = sample.ToCsharpCode("varName", 2, 3);
        Assert.That(subset, Is.EqualTo(
            "var varName = new byte[] {0x03, 0x64, 0xC8};"), "subset");
    }
    
    [Test]
    public void output_bytes_hex_description()
    {
        var sample = new byte[] {
            1,   2,   3,   4,  5,  6,  7,  8,  9,  10, 11, 12, 13, 14, 15, 16,
            17,  18,  19,  20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32,
            255, 255, 255, 0
        };
        
        var fullOutput = sample.Describe("name of thing");
        Console.WriteLine(fullOutput);
        Assert.That(fullOutput, Is.EqualTo(
            "name of thing => 36bytes\r\n" +
            "0000: 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F 10 \r\n" +
            "0016: 11 12 13 14 15 16 17 18 19 1A 1B 1C 1D 1E 1F 20 \r\n" +
            "0032: FF FF FF 00 \r\n"), "full");
        
        var subset = sample.Describe("name of thing", 10, 25);
        Assert.That(subset, Is.EqualTo(
            "name of thing => 36bytes\r\n" +
            "0010: 0B 0C 0D 0E 0F 10 11 12 13 14 15 16 17 18 19 1A \r\n" +
            "0026: 1B 1C 1D 1E 1F 20 FF FF FF 00 \r\n"), "subset");
    }
}
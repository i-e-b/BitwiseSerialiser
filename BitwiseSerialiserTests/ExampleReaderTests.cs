using BitwiseSerialiser;
using ExampleFontReader;
using NUnit.Framework;

namespace BitwiseSerialiserTests;

#pragma warning disable CS8602
[TestFixture]
public class ExampleReaderTests
{
    [Test]
    public void read_font_file()
    {
        var font = FontFile.FromFile("dave.ttf");
        Console.WriteLine(TypeDescriber.Describe(font));
    }
}
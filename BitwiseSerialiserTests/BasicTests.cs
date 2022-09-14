using BitwiseSerialiser;
using NUnit.Framework;

namespace BitwiseSerialiserTests;

[TestFixture]
public class BasicTests
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
}
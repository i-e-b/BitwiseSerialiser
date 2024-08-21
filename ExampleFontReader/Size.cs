using System.Diagnostics.CodeAnalysis;

namespace ExampleFontReader;

/// <summary>
/// From https://learn.microsoft.com/en-us/typography/opentype/spec/otff#dataTypes
/// <p/>
/// All OpenType fonts use big-endian (network) byte order
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
public static class Size
{
    public const int uint32 = 4;
    public const int Offset32 = 4;
    public const int uint16 = 2;
    
    /// <summary> 4 byte ASCII string </summary>
    public const int Tag = 4;

    /// <summary> From FontForge documents </summary>
    public const int I = 4;

    /// <summary> From FontForge documents </summary>
    public const int Q = 8;
}
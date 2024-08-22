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
    /// <summary>32-bit unsigned integer</summary>
    public const int uint32 = 4;
    
    /// <summary>Long offset to a table, same as uint32, NULL offset = 0x00000000</summary>
    public const int Offset32 = 4;
    
    /// <summary>32-bit signed fixed-point number (16.16)</summary>
    public const int Fixed = 4;
    
    /// <summary>16-bit unsigned integer</summary>
    public const int uint16 = 2;

    /// <summary>16-bit signed integer</summary>
    public const int int16 = 2;
    
    /// <summary>Short offset to a table, same as uint16, NULL offset = 0x0000</summary>
    public const int Offset16 = 2;
    
    /// <summary>int16 that describes a quantity in font design units</summary>
    public const int FWORD = 2;
    
    /// <summary>uint16 that describes a quantity in font design units</summary>
    public const int UFWORD = 2;

    /// <summary>
    /// Date and time represented in number of seconds since 12:00 midnight, January 1, 1904, UTC. The value is represented as a signed 64-bit integer.
    /// </summary>
    public const int LONGDATETIME = 8;
    
    /// <summary> 4 byte ASCII string </summary>
    public const int Tag = 4;

    /// <summary> From FontForge documents </summary>
    public const int I = 4;

    /// <summary> From FontForge documents. See <see cref="LONGDATETIME"/> </summary>
    public const int Q = 8;
}
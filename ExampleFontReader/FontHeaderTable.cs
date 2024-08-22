using BitwiseSerialiser;

namespace ExampleFontReader;

/// <summary>
/// https://learn.microsoft.com/en-us/typography/opentype/spec/head
/// </summary>
[ByteLayout]
public class FontHeaderTable : GeneralTable
{
    public const string TableTag = "head";
    private static readonly DateTime OpenTypeEpoch = new(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    
    /// <summary>
    /// Major version number of the font header table — set to 1.
    /// </summary>
    [BigEndian(bytes: Size.uint16, order: 0)]
    public int MajorVersion;
    
    /// <summary>
    /// Minor version number of the font header table — set to 0.
    /// </summary>
    [BigEndian(bytes: Size.uint16, order: 1)]
    public int MinorVersion;

    /// <summary>
    /// Set by font manufacturer.
    /// </summary>
    [BigEndian(bytes: Size.Fixed, order: 2)]
    public long FontRevision;
    
    /// <summary>
    /// To compute: set it to 0, sum the entire font as uint32, then store 0xB1B0AFBA - sum.
    /// If the font is used as a component in a font collection file, the value of this field will be invalidated by changes to the file structure and font table directory, and must be ignored.
    /// </summary>
    [BigEndian(bytes: Size.uint32, order: 3)]
    public long ChecksumAdjust;
    
    /// <summary>
    /// Set to 0x5F0F3CF5.
    /// </summary>
    [BigEndian(bytes: Size.uint32, order: 4), FixedValue(0x5F,0x0F,0x3C,0xF5)]
    public long MagicNumber;
    
    /// <summary>
    /// See https://learn.microsoft.com/en-us/typography/opentype/spec/head
    /// </summary>
    [BigEndian(bytes: Size.uint16, order: 5)]
    public int Flags;
    
    /// <summary>
    /// Set to a value from 16 to 16384. Any value in this range is valid. In fonts that have TrueType outlines, a power of 2 is recommended as this allows performance optimization in some rasterizers.
    /// </summary>
    [BigEndian(bytes: Size.uint16, order: 6)]
    public int UnitsPerEm;
    
    /// <summary>
    /// File creation date
    /// </summary>
    [BigEndian(bytes: Size.LONGDATETIME, order: 7)]
    public uint SourceCreated;
    
    /// <summary>
    /// Last modification to file
    /// </summary>
    [BigEndian(bytes: Size.LONGDATETIME, order: 8)]
    public uint SourceModified;

    /// <summary>
    /// Minimum x coordinate across all glyph bounding boxes.
    /// </summary>
    [BigEndian(bytes: Size.int16, order: 9)]
    public int XMin;
    
    /// <summary>
    /// Minimum y coordinate across all glyph bounding boxes.
    /// </summary>
    [BigEndian(bytes: Size.int16, order: 10)]
    public int YMin;
    
    /// <summary>
    /// Maximum x coordinate across all glyph bounding boxes.
    /// </summary>
    [BigEndian(bytes: Size.int16, order: 11)]
    public int XMax;
    
    /// <summary>
    /// Maximum y coordinate across all glyph bounding boxes.
    /// </summary>
    [BigEndian(bytes: Size.int16, order: 12)]
    public int YMax;
    
    /// <summary>
    /// True if set to 1:
    /// Bit 0: Bold;
    /// Bit 1: Italic;
    /// Bit 2: Underline;
    /// Bit 3: Outline;
    /// Bit 4: Shadow;
    /// Bit 5: Condensed;
    /// Bit 6: Extended;
    /// Bits 7 – 15: Reserved (set to 0).
    /// </summary>
    [BigEndian(bytes: Size.uint16, order: 13)]
    public int MacStyleFlags;
    
    /// <summary>
    /// Smallest readable size in pixels
    /// </summary>
    [BigEndian(bytes: Size.uint16, order: 14)]
    public int SmallestReadableSizeInPixels;
    
    /// <summary>
    /// Deprecated (Set to 2).
    /// </summary>
    [BigEndian(bytes: Size.int16, order: 15)]
    public int FontDirectionHint;
    
    /// <summary>
    /// 0 for short offsets (Offset16), 1 for long (Offset32).
    /// </summary>
    [BigEndian(bytes: Size.int16, order: 16)]
    public int IndexToLocFormat;
    
    /// <summary>
    /// 0 for current format.
    /// </summary>
    [BigEndian(bytes: Size.int16, order: 17)]
    public int GlyphDataFormat;
    
    public double FontRevisionReal => FontRevision / 65536.0;
    public DateTime Created => OpenTypeEpoch.AddSeconds(SourceCreated);
    public DateTime Modified => OpenTypeEpoch.AddSeconds(SourceModified);
}
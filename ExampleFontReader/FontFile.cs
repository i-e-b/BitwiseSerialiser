using System.Diagnostics.CodeAnalysis;
using BitwiseSerialiser;

namespace ExampleFontReader;

/// <summary>
/// Read and interpret a font file.
/// <p/>
/// Specification from https://learn.microsoft.com/en-us/typography/opentype/spec/otff
/// </summary>
[ByteLayout]
public class FontFile
{
    /// <summary>
    /// Read a font file from a file path
    /// </summary>
    public static FontFile FromFile(string filePath)
    {
        var bytes = File.ReadAllBytes(filePath);
        
        // Read the main index
        var ok = ByteSerialiser.FromBytes<FontFile>(bytes, out var result);
        if (!ok) throw new Exception("Could not interpret file");

        // Read each table from the index
        foreach (var tableIndex in result.TableDirectory)
        {
            result.Tables.Add(GeneralTable.BuildFromDirectory(tableIndex, bytes));
        }

        return result;
    }

    /// <summary>
    /// 0x00010000 or 0x4F54544F ('OTTO')
    /// </summary>
    [BigEndian(bytes: Size.uint32, order:0)]
    public uint SfntVersion;

    /// <summary>
    /// Number of tables in the file
    /// </summary>
    [BigEndian(bytes: Size.uint16, order: 1)]
    public int NumTables;

    /// <summary>
    /// Maximum power of 2 less than or equal to numTables,
    /// times 16 ((2**floor(log2(numTables))) * 16, where “**” is an exponentiation operator).
    /// </summary>
    [BigEndian(bytes: Size.uint16, order: 2)]
    public int SearchRange;
    
    /// <summary>
    /// Log2 of the maximum power of 2 less than or equal to numTables (log2(searchRange/16), which is equal to floor(log2(numTables))).
    /// </summary>
    [BigEndian(bytes: Size.uint16, order: 3)]
    public int EntrySelector;

    /// <summary>
    /// numTables times 16, minus searchRange ((numTables * 16) - searchRange).
    /// </summary>
    [BigEndian(bytes: Size.uint16, order: 4)]
    public int RangeShift;

    [ByteLayoutVariableChild(source: nameof(GetTableCount), order: 5)]
    public TableDirectoryEntry[] TableDirectory = [];

    /// <summary>
    /// Tables recovered from the table directory
    /// </summary>
    public List<GeneralTable> Tables { get; set; } = new();
    
    public int GetTableCount() => NumTables;
}

[SuppressMessage("ReSharper", "InconsistentNaming")]
public abstract class GeneralTable
{
    public static GeneralTable BuildFromDirectory(TableDirectoryEntry index, byte[] bytes)
    {
        return index.Tag switch
        {
            FontForgeTimeStampTable.TableTag => Build<FontForgeTimeStampTable>(index, bytes),
            Os2WindowsMetricsTable.TableTag => Build<Os2WindowsMetricsTable>(index, bytes),
            _ => new UnknownTable($"Unknown table type '{index.Tag}'")
        };
    }

    private static GeneralTable Build<T>(TableDirectoryEntry index, byte[] bytes) where T : new()
    {
        ByteSerialiser.FromBytes<T>(bytes, (int)index.Offset, (int)index.Length, out var result);
        return result as GeneralTable ?? new UnknownTable($"Could not cast '{typeof(T).Name}' to a table");
    }
}

/// <summary>
/// Placeholder for tables that could not be read, or whose type is not handled
/// </summary>
public class UnknownTable : GeneralTable
{
    public string Message { get; set; }
    
    public UnknownTable(string msg)
    {
        Message = msg;
    }
}

/// <summary>
/// https://fonttools.readthedocs.io/en/latest/_modules/fontTools/ttLib/tables/F_F_T_M_.html#table_F_F_T_M_
/// </summary>
[ByteLayout]
public class FontForgeTimeStampTable:GeneralTable
{
    public const string TableTag = "FFTM";
    public string Type => TableTag;
    private static readonly DateTime FontForgeEpoch = new(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    
    [BigEndian(bytes: Size.I, order: 4)]
    public uint Version;
    
    [BigEndian(bytes: Size.Q, order: 5)]
    public uint FfTimeStamp;
    
    [BigEndian(bytes: Size.Q, order: 6)]
    public uint SourceCreated;
    
    [BigEndian(bytes: Size.Q, order: 7)]
    public uint SourceModified;

    public DateTime TimeStamp => FontForgeEpoch.AddSeconds(FfTimeStamp);
    public DateTime Created => FontForgeEpoch.AddSeconds(SourceCreated);
    public DateTime Modified => FontForgeEpoch.AddSeconds(SourceModified);
}

/// <summary>
/// https://learn.microsoft.com/en-us/typography/opentype/spec/os2#os2-table-formats
/// </summary>
[ByteLayout]
public class Os2WindowsMetricsTable : GeneralTable
{
    public const string TableTag = "OS/2";
    public string Type => TableTag;

    /// <summary>
    /// This should be usable for version 2+
    /// </summary>
    [BigEndian(bytes: Size.uint16, order: 0)]
    public int Version;
    
    /// <summary>
    /// The Average Character Width field specifies the arithmetic average of the escapement (width) of all non-zero width glyphs in the font.
    /// https://learn.microsoft.com/en-us/typography/opentype/spec/os2#xavgcharwidth
    /// </summary>
    [BigEndian(bytes: Size.FWORD, order: 1)]
    public int XAvgCharWidth;

    /// <summary>
    /// Indicates the visual weight (degree of blackness or thickness of strokes) of the characters in the font. Values from 1 to 1000 are valid.
    /// https://learn.microsoft.com/en-us/typography/opentype/spec/os2#usweightclass
    /// </summary>
    [BigEndian(bytes: Size.uint16, order: 2)]
    public int UsWeightClass;
    
    /// <summary>
    /// Indicates a relative change from the normal aspect ratio (width to height ratio) as specified by a font designer for the glyphs in a font.
    /// https://learn.microsoft.com/en-us/typography/opentype/spec/os2#uswidthclass
    /// </summary>
    [BigEndian(bytes: Size.uint16, order: 3)]
    public int UsWidthClass;
    
    /// <summary>
    /// Indicates font embedding licensing rights for the font
    /// https://learn.microsoft.com/en-us/typography/opentype/spec/os2#fstype
    /// </summary>
    [BigEndian(bytes: Size.uint16, order: 4)]
    public int FsType;
    
    /// <summary>
    /// The recommended horizontal size in font design units for subscripts for this font. Should be > 0
    /// https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ysubscriptxsize
    /// </summary>
    [BigEndian(bytes: Size.FWORD, order: 5)]
    public int YSubscriptXSize;
    
    /// <summary>
    /// The recommended vertical size in font design units for subscripts for this font. Should be > 0
    /// https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ysubscriptysize
    /// </summary>
    [BigEndian(bytes: Size.FWORD, order: 6)]
    public int YSubscriptYSize;
    
    /// <summary>
    /// The recommended horizontal offset in font design units for subscripts for this font.
    /// https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ysubscriptxoffset
    /// </summary>
    [BigEndian(bytes: Size.FWORD, order: 7)]
    public int YSubscriptXOffset;
    
    /// <summary>
    /// The recommended vertical offset in font design units from the baseline for subscripts for this font
    /// https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ysubscriptyoffset
    /// </summary>
    [BigEndian(bytes: Size.FWORD, order: 8)]
    public int YSubscriptYOffset;
    
    /// <summary>
    /// The recommended vertical offset in font design units from the baseline for subscripts for this font
    /// https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ysubscriptyoffset
    /// </summary>
    [BigEndian(bytes: Size.FWORD, order: 9)]
    public int YSuperscriptXSize;
    
    /// <summary>
    /// The recommended vertical size in font design units for superscripts for this font. Should be > 0
    /// https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ysuperscriptysize
    /// </summary>
    [BigEndian(bytes: Size.FWORD, order: 10)]
    public int YSuperscriptYSize;
    
    /// <summary>
    /// The recommended horizontal offset in font design units for superscripts for this font.
    /// https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ysuperscriptxoffset
    /// </summary>
    [BigEndian(bytes: Size.FWORD, order: 11)]
    public int YSuperscriptXOffset;
    
    /// <summary>
    /// The recommended vertical offset in font design units from the baseline for superscripts for this font.
    /// https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ysuperscriptyoffset
    /// </summary>
    [BigEndian(bytes: Size.FWORD, order: 12)]
    public int YSuperscriptYOffset;
    
    /// <summary>
    /// Thickness of the strikeout stroke in font design units. Should be > 0.
    /// https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ystrikeoutsize
    /// </summary>
    [BigEndian(bytes: Size.FWORD, order: 13)]
    public int YStrikeoutSize;
    
    /// <summary>
    /// The position of the top of the strikeout stroke relative to the baseline in font design units.
    /// https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ystrikeoutposition
    /// </summary>
    [BigEndian(bytes: Size.FWORD, order: 14)]
    public int YStrikeoutPosition;
    
    /// <summary>
    /// This field provides a classification of font-family design.
    /// https://learn.microsoft.com/en-us/typography/opentype/spec/os2#sfamilyclass
    /// https://learn.microsoft.com/en-us/typography/opentype/spec/ibmfc
    /// </summary>
    [BigEndian(bytes: Size.int16, order: 15)]
    public int FamilyClass;
    
    /// <summary>
    /// This 10-byte array of numbers is used to describe the visual characteristics of a given typeface. These characteristics are then used to associate the font with other fonts of similar appearance having different names.
    /// https://learn.microsoft.com/en-us/typography/opentype/spec/os2#panose
    /// </summary>
    [ByteString(bytes: 10, order: 16)]
    public byte[] Panose = []; // TODO: child object
    
    /// <summary>
    /// This field is used to specify the Unicode blocks or ranges encompassed by the font file
    /// https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ur
    /// </summary>
    [ByteString(bytes: Size.uint32 * 4, order: 17)]
    public byte[] UnicodeRanges = [];

    /// <summary>
    /// The four character identifier for the vendor of the given type face.
    /// https://learn.microsoft.com/en-us/typography/opentype/spec/os2#achvendid
    /// </summary>
    [AsciiString(bytes: Size.Tag, order: 18)]
    public string VendorId = "";

    /// <summary>
    /// Contains information concerning the nature of the font patterns
    /// https://learn.microsoft.com/en-us/typography/opentype/spec/os2#fsselection
    /// </summary>
    [BigEndian(bytes: Size.int16, order: 19)]
    public int FontStyleFlags; // TODO: convert flags to/from a C# enum
    
    /// <summary>
    /// The minimum Unicode index (character code) in this font, according to the 'cmap' subtable for platform ID 3 and platform- specific encoding ID 0 or 1. For most fonts supporting Win-ANSI or other character sets, this value would be 0x0020.
    /// https://learn.microsoft.com/en-us/typography/opentype/spec/os2#usfirstcharindex
    /// </summary>
    [BigEndian(bytes: Size.int16, order: 20)]
    public int FirstCharIndex;
    
    /// <summary>
    /// The maximum Unicode index (character code) in this font, according to the 'cmap' subtable for platform ID 3 and encoding ID 0 or 1. This value depends on which character sets the font supports
    /// https://learn.microsoft.com/en-us/typography/opentype/spec/os2#uslastcharindex
    /// </summary>
    [BigEndian(bytes: Size.int16, order: 21)]
    public int LastCharIndex;
}
using BitwiseSerialiser;

namespace ExampleFontReader;

/// <summary>
/// https://learn.microsoft.com/en-us/typography/opentype/spec/cmap
/// </summary>
[ByteLayout]
public class CharacterToGlyphIndexMappingTable : GeneralTable
{
    public const string TableTag = "cmap";

    /// <summary>
    /// Table version number (0).
    /// </summary>
    [BigEndian(bytes: Size.uint16, order: 1)]
    public uint Version;

    /// <summary>
    /// Number of encoding tables that follow.
    /// </summary>
    [BigEndian(bytes: Size.uint16, order: 2)]
    public int NumTables;

    // TODO: Add an 'SpecialiseWith' source to ByteLayoutVariableChild
    [ByteLayoutVariableChild(nameof(CountHowMany), order: 3)]
    public EncodingIndex[]? EncodingRecords;

    public int CountHowMany() => NumTables;

    public int[] TableOffsets => EncodingRecords?.Select(r=>r.SubTableOffset).ToArray() ?? [];

    // TODO: Add an 'Offsets' source to ByteLayoutMultiChild. Note: we need a way to say where the offset is from
    //       [ByteLayoutMultiChild(order: 4, OffsetsFromStart = nameof(TableOffsets))] // offsets are from the start of the parent
    //       [ByteLayoutMultiChild(order: 4, OffsetsFromHere = nameof(TableOffsets))] // offsets are from the end of previous field
    //       [ByteLayoutMultiChild(order: 4, OffsetsInAllData = nameof(TableOffsets))] // offsets are from the start of the supplied data (entire file)


    /*
    [ByteLayoutChildMultiFromOffset(order: 4, OffsetsFromStart = nameof(TableOffsets), SpecialiseWith = nameof(TableSpecialise))]
    public GeneralEncodingTable[]? EncodingTables; //

    public Type TableSpecialise(int index)
    {
        var table = EncodingRecords?[index] ?? throw new Exception("Invalid index");
        var realKey = (table.PlatformId << 16) + table.EncodingId;
        return realKey switch
        {
            -2 => typeof(Format0_ByteEncodingTable),
            -1 => typeof(Option2Child),
            _ => throw new Exception("Unmapped child") // this is fine if we must handle all variants
        };
    }
    */
}

[ByteLayout]
public class GeneralEncodingTable { }

/// <summary>
/// https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-0-byte-encoding-table
/// </summary>
[ByteLayout]
public class Format0_ByteEncodingTable: GeneralEncodingTable {
}

/// <summary>
/// https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#encoding-records-and-encodings
/// </summary>
[ByteLayout]
public class EncodingIndex
{
    /// <summary>
    /// Platform ID.
    /// </summary>
    [BigEndian(bytes: Size.uint16, order: 1)]
    public uint PlatformId;

    /// <summary>
    /// Platform-specific encoding ID.
    /// </summary>
    [BigEndian(bytes: Size.uint16, order: 2)]
    public int EncodingId;

    /// <summary>
    /// Byte offset from beginning of table to the sub-table for this encoding.
    /// </summary>
    [BigEndian(bytes: Size.Offset32, order: 3)]
    public int SubTableOffset;
}
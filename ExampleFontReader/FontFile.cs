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
        
        var ok = ByteSerialiser.FromBytes<FontFile>(bytes, out var result);

        if (!ok) throw new Exception("Could not interpret file");
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
    public GenericTable[] Table0 = [];
    
    public int GetTableCount() => NumTables;
}
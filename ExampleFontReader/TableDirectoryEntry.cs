using BitwiseSerialiser;

namespace ExampleFontReader;

[ByteLayout]
public class TableDirectoryEntry
{
    /// <summary>
    /// Tag of the table
    /// </summary>
    [AsciiString(bytes: Size.Tag, order: 0)]
    public string Tag = "";

    /// <summary>
    /// Checksum of the target table
    /// </summary>
    /// <example><code><![CDATA[
    /// uint32 CalcTableChecksum(uint32 *Table, uint32 Length) {
    ///    uint32 Sum = 0L;
    ///    uint32 *Endptr = Table+((Length+3) & ~3) / sizeof(uint32);
    ///    while (Table < EndPtr) {
    ///        Sum += *Table++;
    ///    }
    ///    return Sum;
    ///}
    /// ]]></code></example>
    [BigEndian(bytes: Size.uint32, order: 1)]
    public uint Checksum;

    /// <summary>
    /// Offset of the table from start of file
    /// </summary>
    [BigEndian(bytes: Size.Offset32, order: 2)]
    public uint Offset;

    /// <summary>
    /// Length of the table
    /// </summary>
    [BigEndian(bytes: Size.uint32, order: 3)]
    public uint Length;
}
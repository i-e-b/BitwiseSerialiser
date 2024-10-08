﻿using System.Diagnostics.CodeAnalysis;
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
    /// <summary>
    /// Original index entry for this table
    /// </summary>
    public TableDirectoryEntry? TableOffset { get; set; }

    /// <summary>
    /// Dispatcher for Tag -> C# type
    /// </summary>
    public static GeneralTable BuildFromDirectory(TableDirectoryEntry index, byte[] bytes)
    {
        return index.Tag switch
        {
            FontForgeTimeStampTable.TableTag => Build<FontForgeTimeStampTable>(index, bytes),
            Os2WindowsMetricsTable.TableTag => Build<Os2WindowsMetricsTable>(index, bytes),
            ControlValueProgramTable.TableTag => Build<ControlValueProgramTable>(index, bytes),
            CharacterToGlyphIndexMappingTable.TableTag => Build<CharacterToGlyphIndexMappingTable>(index, bytes),
            FontHeaderTable.TableTag => Build<FontHeaderTable>(index, bytes),
            _ => new UnknownTable($"Unknown table type '{index.Tag}'")
        };
    }

    private static GeneralTable Build<T>(TableDirectoryEntry index, byte[] bytes) where T : new()
    {
        ByteSerialiser.FromBytes<T>(bytes, (int)index.Offset, (int)index.Length, out var result);
        var output = result as GeneralTable ?? new UnknownTable($"Could not cast '{typeof(T).Name}' to a table");
        output.TableOffset = index;
        return output;
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
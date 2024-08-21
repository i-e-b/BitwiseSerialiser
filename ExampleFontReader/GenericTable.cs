using BitwiseSerialiser;

namespace ExampleFontReader;

[ByteLayout(SpecialiseWith = nameof(TableSpecialise))]
public class GenericTable
{
    [AsciiString(bytes: Size.Tag, order: 0)]
    public string Tag = "";

    [BigEndian(bytes: Size.uint32, order: 1)]
    public uint Checksum;

    [BigEndian(bytes: Size.Offset32, order: 2)]
    public uint Offset;

    [BigEndian(bytes: Size.uint32, order: 3)]
    public uint Length;

    public Type? TableSpecialise()
    {
        return Tag switch
        {
            FontForgeTimeStampTable.TableTag => typeof(FontForgeTimeStampTable),
            _ => null
        };
    }
}

/// <summary>
/// https://fonttools.readthedocs.io/en/latest/_modules/fontTools/ttLib/tables/F_F_T_M_.html#table_F_F_T_M_
/// </summary>
public class FontForgeTimeStampTable : GenericTable
{
    public const string TableTag = "FFTM";
    
    [BigEndian(bytes: Size.I, order: 0)]
    public uint Version;
    
    [BigEndian(bytes: Size.Q, order: 1)]
    public uint FfTimeStamp;
    
    [BigEndian(bytes: Size.Q, order: 2)]
    public uint SourceCreated;
    
    [BigEndian(bytes: Size.Q, order: 3)]
    public uint SourceModified;
    
}
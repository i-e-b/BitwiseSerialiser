using BitwiseSerialiser;

namespace ExampleFontReader;

/// <summary>
/// https://fonttools.readthedocs.io/en/latest/_modules/fontTools/ttLib/tables/F_F_T_M_.html#table_F_F_T_M_
/// </summary>
[ByteLayout]
public class FontForgeTimeStampTable:GeneralTable
{
    public const string TableTag = "FFTM";
    private static readonly DateTime FontForgeEpoch = new(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    
    [BigEndian(bytes: Size.I, order: 0)]
    public uint Version;
    
    [BigEndian(bytes: Size.Q, order: 1)]
    public uint FfTimeStamp;
    
    [BigEndian(bytes: Size.Q, order: 2)]
    public uint SourceCreated;
    
    [BigEndian(bytes: Size.Q, order: 3)]
    public uint SourceModified;

    public DateTime TimeStamp => FontForgeEpoch.AddSeconds(FfTimeStamp);
    public DateTime Created => FontForgeEpoch.AddSeconds(SourceCreated);
    public DateTime Modified => FontForgeEpoch.AddSeconds(SourceModified);
}
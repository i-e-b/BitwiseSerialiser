using BitwiseSerialiser;

namespace ExampleFontReader;

/// <summary>
/// https://learn.microsoft.com/en-us/typography/opentype/spec/prep
/// </summary>
[ByteLayout]
public class ControlValueProgramTable : GeneralTable
{
    public const string TableTag = "prep";

    /// <summary>
    /// The Control Value (CV) Program consists of a set of TrueType instructions that can be used to make font-wide changes in the Control Value Table. Any instruction is valid in the CV Program but since no glyph is associated with it, instructions intended to move points within a particular glyph outline have no effect in the CV Program.
    /// </summary>
    [RemainingBytes(order: 0)]
    public byte[] Program = [];
}
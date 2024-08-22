using BitwiseSerialiser;

namespace ExampleFontReader;

/// <summary>
/// https://learn.microsoft.com/en-us/typography/opentype/spec/os2#os2-table-formats
/// </summary>
[ByteLayout]
public class Os2WindowsMetricsTable : GeneralTable
{
    public const string TableTag = "OS/2";

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
    
    /// <summary>
    /// The typographic ascender for this font. This field should be combined with the sTypoDescender and sTypoLineGap values to determine default line spacing.
    /// https://learn.microsoft.com/en-us/typography/opentype/spec/os2#stypoascender
    /// </summary>
    [BigEndian(bytes: Size.FWORD, order: 22)]
    public int TypoAscender;
    
    /// <summary>
    /// The typographic descender for this font. This field should be combined with the sTypoAscender and sTypoLineGap values to determine default line spacing
    /// https://learn.microsoft.com/en-us/typography/opentype/spec/os2#stypodescender
    /// </summary>
    [BigEndian(bytes: Size.FWORD, order: 23)]
    public int TypoDescender;
    
    /// <summary>
    /// The typographic line gap for this font. This field should be combined with the sTypoAscender and sTypoDescender values to determine default line spacing.
    /// https://learn.microsoft.com/en-us/typography/opentype/spec/os2#stypolinegap
    /// </summary>
    [BigEndian(bytes: Size.FWORD, order: 24)]
    public int TypoLineGap;
    
    /// <summary>
    /// The “Windows ascender” metric. This should be used to specify the height above the baseline for a clipping region.
    /// In the Windows GDI implementation, the usWinAscent and usWinDescent values have been used to determine the size of the bitmap surface in the TrueType rasterizer
    /// https://learn.microsoft.com/en-us/typography/opentype/spec/os2#uswinascent
    /// </summary>
    [BigEndian(bytes: Size.UFWORD, order: 25)]
    public int WinAscent;
    
    /// <summary>
    /// The “Windows descender” metric. This should be used to specify the vertical extent below the baseline for a clipping region.
    /// n the Windows GDI implementation, the usWinDescent and usWinAscent values have been used to determine the size of the bitmap surface in the TrueType rasterizer
    /// https://learn.microsoft.com/en-us/typography/opentype/spec/os2#uswindescent
    /// </summary>
    [BigEndian(bytes: Size.UFWORD, order: 26)]
    public int WinDecent;
    
    /// <summary>
    /// [Version 1+]
    /// This field is used to specify the code pages encompassed by the font file
    /// https://learn.microsoft.com/en-us/typography/opentype/spec/os2#cpr
    /// </summary>
    [ByteString(bytes: Size.uint32 * 2, order: 27)]
    public byte[] CodePageCharacterRange = [];
    
    /// <summary>
    /// [Version 2+]
    /// This metric specifies the distance between the baseline and the approximate height of non-ascending lowercase letters measured in font design units.
    /// https://learn.microsoft.com/en-us/typography/opentype/spec/os2#sxheight
    /// </summary>
    [BigEndian(bytes: Size.FWORD, order: 28)]
    public int XHeight;
    
    /// <summary>
    /// [Version 2+]
    /// This metric specifies the distance between the baseline and the approximate height of uppercase letters measured in font design units. This value would normally be specified by a type designer but in situations where that is not possible, for example when a legacy font is being converted, the value may be set equal to the top of the unscaled and unhinted glyph bounding box of the glyph encoded at U+0048 (LATIN CAPITAL LETTER H). If no glyph is encoded in this position the field should be set to 0.
    /// https://learn.microsoft.com/en-us/typography/opentype/spec/os2#scapheight
    /// </summary>
    [BigEndian(bytes: Size.FWORD, order: 29)]
    public int CapHeight;

    /// <summary>
    /// [Version 2+]
    /// This is the Unicode code point, in UTF-16 encoding, of a character that can be used for a default glyph if a requested character is not supported in the font. If the value of this field is zero, glyph ID 0 is to be used for the default character. This field cannot represent supplementary-plane character values (code points greater than 0xFFFF), and so applications are strongly discouraged from using this field.
    /// https://learn.microsoft.com/en-us/typography/opentype/spec/os2#usdefaultchar
    /// </summary>
    [BigEndian(bytes: Size.uint16, order: 30)]
    public int DefaultChar;
    
    /// <summary>
    /// [Version 2+]
    /// This is the Unicode code point, in UTF-16 encoding, of a character that can be used as a default break character. The break character is used to separate words and justify text. Most fonts specify U+0020 SPACE as the break character. This field cannot represent supplementary-plane character values (code points greater than 0xFFFF), and so applications are strongly discouraged from using this field.
    /// https://learn.microsoft.com/en-us/typography/opentype/spec/os2#usbreakchar
    /// </summary>
    [BigEndian(bytes: Size.uint16, order: 31)]
    public int BreakChar;
    
    /// <summary>
    /// [Version 2+]
    /// The maximum length of a target glyph context for any feature in this font. For example, a font which has only a pair kerning feature should set this field to 2. If the font also has a ligature feature in which the glyph sequence “f f i” is substituted by the ligature “ffi”, then this field should be set to 3
    /// https://learn.microsoft.com/en-us/typography/opentype/spec/os2#usmaxcontext
    /// </summary>
    [BigEndian(bytes: Size.uint16, order: 32)]
    public int MaxContext;
    
    /// <summary>
    /// [Version 5]
    /// This field is used for fonts with multiple optical styles.
    /// This value is the lower value of the size range for which this font has been designed. The units for this field are TWIPs (one-twentieth of a point, or 1440 per inch). The value is inclusive — meaning that that font was designed to work best at this point size through, but not including, the point size indicated by usUpperOpticalPointSize
    /// https://learn.microsoft.com/en-us/typography/opentype/spec/os2#lps
    /// </summary>
    [BigEndian(bytes: Size.uint16, order: 33)]
    public int LowerOpticalPointSize;
    
    /// <summary>
    /// [Version 5]
    /// This field is used for fonts with multiple optical styles.
    /// This value is the upper value of the size range for which this font has been designed. The units for this field are TWIPs (one-twentieth of a point, or 1440 per inch). The value is exclusive — meaning that that font was designed to work best below this point size down to the usLowerOpticalPointSize threshold. When used with other optical-size-variant fonts within a typographic family that also specify usLowerOpticalPointSize and usUpperOpticalPointSize values, it would be expected that another font has the usLowerOpticalPointSize field set to the same value as the value in this field, unless this font is designed for the highest size range among the fonts in the family
    /// https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ups
    /// </summary>
    [BigEndian(bytes: Size.uint16, order: 34)]
    public int UpperOpticalPointSize;
}
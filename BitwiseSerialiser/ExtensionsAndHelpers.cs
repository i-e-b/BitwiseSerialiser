using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace BitwiseSerialiser;

/// <summary>
/// Byte and bit twiddling
/// </summary>
public static class BinaryExtensions
{
    /// <summary>
    /// Translate a BCD value to a standard integer encoding
    /// Returns true if the value is fully valid, false otherwise.
    /// </summary>
    public static bool BcdToDec(this byte bcd, out int dec)
    {
        var lower = bcd & 0x0F;
        var upper = (bcd >> 4) & 0x0F;
            
        dec = lower + (upper * 10);
            
        return lower < 0x0A && upper < 0x0A;
    }

    /// <summary>
    /// Translate a standard integer encoding to BCD.
    /// Values 0..99 supported, others will be wrapped.
    /// </summary>
    public static byte DecToBcd(this int value)
    {
        var lower = value % 10;
        var upper = (value / 10) % 10;
            
        return (byte)(lower + (upper << 4));
    }
    
    /// <summary> Render a human-friendly string for a file size in bytes </summary>
    public static string Human(this ulong byteCount) => HumanStr(byteCount);
    
    /// <summary> Render a human-friendly string for a file size in bytes </summary>
    public static string Human(this long byteCount) => HumanStr((ulong)byteCount);
    
    /// <summary> Render a human-friendly string for a file size in bytes </summary>
    public static string Human(this uint byteCount) => HumanStr(byteCount);
    
    /// <summary> Render a human-friendly string for a file size in bytes </summary>
    public static string Human(this int byteCount) => HumanStr((ulong)byteCount);
    
    /// <summary> Render a human-friendly string for a file size in bytes </summary>
    public static string Human(this ushort byteCount) => HumanStr(byteCount);
    
    /// <summary> Render a human-friendly string for a file size in bytes </summary>
    public static string Human(this short byteCount) => HumanStr((ulong)byteCount);
        
    private static string HumanStr(ulong byteLength)
    {
        double size = byteLength;
        var prefix = new []{ "b", "kb", "mb", "gb", "tb", "pb" };
        int i;
        for (i = 0; i < prefix.Length; i++)
        {
            if (size < 1024) break;
            size /= 1024;
        }
        return size.ToString("#0.##") + prefix[i];
    }

    /// <summary>
    /// Write the data from a byte array
    /// into a string that should be valid C# code
    /// </summary>
    /// <param name="bytes">bytes to use</param>
    /// <param name="name">Name of the variable</param>
    /// <param name="offset">start position in the array</param>
    /// <param name="length">number of bytes to serialise</param>
    public static string ToCsharpCode(this byte[]? bytes, string name, int offset, int length)
    {
        var end = offset+length;
        
        name = Safe(name);
        if (bytes is null || length < 1) return $"var {name} = new byte[0];";
            
        var sb = new StringBuilder();
            
        sb.Append("var ");
        sb.Append(name);
        sb.Append(" = new byte[] {");
            
        if (end > bytes.Length) end = bytes.Length;
        for (int b = offset; b < end; b++)
        {
            sb.Append($"0x{bytes[b]:X2}, ");
        }
            
        sb.Append("};");
        sb.AppendLine();
            
        return sb.ToString();
    }

    /// <summary>
    /// Write the data from a byte array
    /// into a string that should be valid C# code
    /// </summary>
    public static string ToCsharpCode(this byte[]? bytes, string name)
    {
        return ToCsharpCode(bytes, name, 0, bytes?.Length ?? 0);
    }

    /// <summary>
    /// Generate a description string for the content of a
    /// byte array, similar to most hex-editors would show.
    /// </summary>
    public static string Describe(this byte[]? bytes, string name)
    {
        return Describe(bytes, name, 0, bytes?.Length ?? 0);
    }
    
    /// <summary>
    /// Generate a description string for the content of a
    /// byte array, similar to most hex-editors would show.
    /// </summary>
    public static string Describe(this byte[]? bytes, string name, int offset, int length)
    {
        var end = offset+length;
        
        if (bytes is null)
        {
            return $"{name} => 0 bytes (null)\r\n";
        }

        var sb = new StringBuilder();

        sb.Append(name);
        sb.Append(" => ");
        sb.Append(bytes.Length);
        sb.Append("bytes");

        var idx = offset;
        if (end > bytes.Length) end = bytes.Length;
        while (idx < end)
        {
            sb.AppendLine();
            sb.Append($"{idx:d4}: ");
            for (int b = 0; (b < 16) && (idx < bytes.Length); b++)
            {
                sb.Append($"{bytes[idx++]:X2} ");
            }
        }

        sb.AppendLine();
        return sb.ToString();
    }

    /// <summary>
    /// Code friendly version of a string
    /// </summary>
    private static string Safe(string name)
    {
        var sb = new StringBuilder();

        var i = 0;
        foreach (var c in name)
        {
            switch (c)
            {
                case >= '0' and <= '9':
                    if (i==0) sb.Append('_');
                    i++;
                    sb.Append(c);
                    break;
                
                case >= 'a' and <= 'z':
                case >= 'A' and <= 'Z':
                case '_':
                    i++;
                    sb.Append(c);
                    break;
                
                case ' ':
                    i++;
                    sb.Append('_');
                    break;
            }
        }
        
        return sb.ToString();
    }

    /// <summary>
    /// Parse a hex string into a byte array
    /// </summary>
    public static byte[] ParseBytes(this string? hexStr)
    {
        if (hexStr is null || hexStr.Length < 1) return Array.Empty<byte>();
        
        var accum = new List<byte>();
        for (int i = 0; i < hexStr.Length; i += 2)
        {
            accum.Add(byte.Parse($"{hexStr[i]}{hexStr[i + 1]}", NumberStyles.HexNumber));
        }
        
        return accum.ToArray();
    }

    /// <summary>
    /// Convert a byte array into a hex string
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public static string HexString(this byte[]? data)
    {
        if (data is null) return "";
        return string.Join("", data.Select(b=>b.ToString("x2")));
    }

    /// <summary>
    /// Write a byte as an 8 character binary string
    /// </summary>
    public static string BinString(this byte b) => Convert.ToString(b, 2).PadLeft(8, '0');
}


/// <summary>
/// Cache with generator
/// </summary>
internal class WeakCache<TK, TV> where TK : notnull
{
    private readonly Func<TK, TV> _generator;
    private readonly Dictionary<TK, TV> _cache = new();

    public WeakCache(Func<TK, TV> generator)
    {
        _generator = generator;
    }

    public TV Get(TK key)
    {
        if (_cache.ContainsKey(key)) return _cache[key];
        var value = _generator(key);
        try
        {
            _cache.Add(key, value);
        }
        catch
        {
            // ignore
        }

        return value;
    }
}

internal static class EnumerableExtensions
{
    public static List<T> ToListOrEmpty<T>(this IEnumerable<T>? src)
    {
        return src is null ? new List<T>() : src.ToList();
    }

    public static IEnumerable<T> OrEmpty<T>(this IEnumerable<T>? src)
    {
        return src is null ? Array.Empty<T>() : src.ToList();
    }
}
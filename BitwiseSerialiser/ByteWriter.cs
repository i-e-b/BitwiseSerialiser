using System;
using System.Collections.Generic;

namespace BitwiseSerialiser;

internal class ByteWriter
{
    private readonly List<byte> _output;
    private int _offset;
    private byte _waiting;

    public ByteWriter()
    {
        _offset = 0;
        _waiting = 0;
        _output = new List<byte>();
    }
        
    public byte[] ToArray()
    {
        // flush last byte?
        if (_offset != 0) _output.Add(_waiting);
            
        // return output
        return _output.ToArray();
    }
        
    public void Add(byte value)
    {
        if (_offset == 0) _output.Add(value);
        else WriteBitsBigEndian(value, 8);
    }

    public void WriteBytesBigEnd(ulong value, int byteCount)
    {
        for (var i = byteCount - 1; i >= 0; i--)
        {
            Add((byte)((value >> (i * 8)) & 0xFF));
        }
    }

    public void WriteBytesLittleEnd(ulong value, int byteCount)
    {
        for (var i = 0; i < byteCount; i++)
        {
            Add((byte)((value >> (i * 8)) & 0xFF));
        }
    }


    /// <summary>
    /// Write a partial byte. While input is not byte aligned,
    /// all writes will be slower
    /// </summary>
    public void WriteBitsBigEndian(ulong value, int bitCount)
    {
        if (bitCount < 1) return;
        if (bitCount > 64) throw new Exception("Invalid bit length in ByteWriter");
            
        var rem = 8 - _offset;
        int shift;
        ulong mask;
            
        // Simple case: will it fit in remaining data of one byte?
        if (bitCount <= rem)
        {
            // example:
            // offset = 3, bit count = 3; value = _ ... _ A B C
            // 0 1 2 3 4 5 6 7
            // x x x A B C _ _
            // next offset = 6
            // _waiting |= (value & b00000111) << 2
                
            shift = rem - bitCount;
            mask = (1ul << bitCount) - 1ul;
            _waiting |= (byte)((value & mask) << shift);
            _offset = (_offset + bitCount) % 8;
            if (_offset == 0)
            {
                _output.Add(_waiting);
                _waiting = 0;
            }
            return;
        }
            
        // Complex case: Data spans multiple bytes
            
        // example 1:
        // offset = 3 (rem = 5), bitCount = 16
        //
        // /-- _waiting --\
        // 0 1 2 3 4 5 6 7 | 0 1 2 3 4 5 6 7 | 0 1 2 3 4 5 6 7
        // x x x A B C D E | F G H I J K L M | N O P _ _ _ _ _
        //
        // bitCount 
        // byte[+0] = ((value & b1111_1xXx__xXxX_xXxX) >> 11)
        // byte[+1] = ((value & b0000_0111__1111_1xXx) >>  3)
        // byte[+2] = ((value & b0000_0000__0000_0111) <<  5)
            
        // example 2:
        // offset = 3 (rem = 5), bitCount = 13
        //
        // /-- _waiting --\
        // 0 1 2 3 4 5 6 7 | 0 1 2 3 4 5 6 7
        // x x x A B C D E | F G H I J K L M
        //
        // bitCount 
        // byte[+0] = ((value & b0001_1111__xXxX_xXxX) >> 8)
        // byte[+1] = ((value & b0000_0000__1111_1111) >>  3)
            
        // first partial
        shift = bitCount - rem;
        mask = (1ul << bitCount) - 1;
        _waiting |= (byte)((value & mask) >> shift);
        _output.Add(_waiting);
        _waiting = 0;
        _offset = 0;
            
        // as many completes as will fit
        while (shift > 0){
            shift -= 8;
            _output.Add((byte)((value >> shift) & 0xff));
        }
            
        // last partial, only if we're going to end un-aligned
        if (shift < 0)
        {
            shift = -shift;
            _offset = 8 - shift;
            _waiting = (byte)((value & 0xff) << shift);
        }
    }
}
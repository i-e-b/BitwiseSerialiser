using System;
using System.IO;

namespace BitwiseSerialiser;

/// <summary>
/// A bitwise wrapper around a byte stream. Also provides run-out
/// </summary>
public class BitwiseStreamWrapper {
    private readonly Stream _original;
    private int _runOutBits;

    private bool _inRunOut;
    private byte _readMask, _writeMask;
    private int _nextOut, _currentIn;

    /// <summary>
    /// Wrap a stream to read and write individual bits
    /// </summary>
    /// <param name="original">Stream that is the source/sink for data</param>
    /// <param name="runOutBits">number of zero bits to make readable after the end of the stream</param>
    public BitwiseStreamWrapper(Stream original, int runOutBits)
    {
        _original = original ?? throw new Exception("Must not wrap a null stream");
        _runOutBits = runOutBits;

        _inRunOut = false;
        _readMask = 1;
        _writeMask = 0x80;
        _nextOut = 0;
        _currentIn = 0;
    }

    /// <summary>
    /// Write the current pending output byte (if any)
    /// </summary>
    public void Flush() {
        if (_writeMask == 0x80) return; // no pending byte
        _original.WriteByte((byte)_nextOut);
        _writeMask = 0x80;
        _nextOut = 0;
    }

    /// <summary>
    /// Write a single bit value to the stream
    /// </summary>
    public void WriteBit(bool value){
        if (value) _nextOut |= _writeMask;
        _writeMask >>= 1;

        if (_writeMask == 0)
        {
            _original.WriteByte((byte)_nextOut);
            _writeMask = 0x80;
            _nextOut = 0;
        }
    }
        
    /// <summary>
    /// Write a single bit value to the stream
    /// </summary>
    public void WriteBit(int value){
        if (value != 0) _nextOut |= _writeMask;
        _writeMask >>= 1;

        if (_writeMask == 0)
        {
            _original.WriteByte((byte)_nextOut);
            _writeMask = 0x80;
            _nextOut = 0;
        }
    }

    /// <summary>
    /// Read a single bit value from the stream.
    /// Returns 1 or 0. Will return all zeros during run-out.
    /// </summary>
    public int ReadBit()
    {
        if (_inRunOut)
        {
            if (_runOutBits-- > 0) return 0;
            throw new Exception("End of input stream");
        }

        if (_readMask == 1)
        {
            _currentIn = _original.ReadByte();
            if (_currentIn < 0)
            {
                _inRunOut = true;
                if (_runOutBits-- > 0) return 0;
                throw new Exception("End of input stream");
            }
            _readMask = 0x80;
        }
        else
        {
            _readMask >>= 1;
        }
        return ((_currentIn & _readMask) != 0) ? 1 : 0;
    }
        
    /// <summary>
    /// Read a single bit value from the stream.
    /// Returns true if data can be read. Does not include run-out
    /// </summary>
    public bool TryReadBit(out int b)
    {
        b=0;
        if (_inRunOut) { return false; }
        if (_readMask == 1)
        {
            _currentIn = _original.ReadByte();
            if (_currentIn < 0) { _inRunOut = true; return false; }
            _readMask = 0x80;
        }
        else
        {
            _readMask >>= 1;
        }
        b=((_currentIn & _readMask) != 0) ? 1 : 0;
        return true;
    }

    /// <summary>
    /// Read 8 bits from the stream. These might not be aligned to a byte boundary
    /// </summary>
    /// <returns></returns>
    public byte ReadByteUnaligned() {
        byte b = 0;
        for (int i = 0x80; i != 0; i >>= 1)
        {
            if (!TryReadBit(out var v)) break;
            b |= (byte)(i * v);
        }
        return b;
    }

    /// <summary>
    /// Write 8 bits to the stream. These might not be aligned to a byte boundary
    /// </summary>
    public void WriteByteUnaligned(byte value) {
        for (int i = 0x80; i != 0; i >>= 1)
        {
            WriteBit((value & i) != 0);
        }
    }
        
    /// <summary>
    /// Write 8 bits to the stream. These will be aligned to a byte boundary. Extra zero bits may be inserted to force alignment
    /// </summary>
    public void WriteByteAligned(byte value) {
        Flush();
        _original.WriteByte(value);
    }

    /// <summary>
    /// Seek underlying stream to start
    /// </summary>
    public void Rewind()
    {
        _original.Seek(0, SeekOrigin.Begin);
            
        _inRunOut = false;
        _readMask = 1;
        _writeMask = 0x80;
        _nextOut = 0;
        _currentIn = 0;
    }

    /// <summary>
    /// Returns true if all real data has been consumed.
    /// Ignores run-out data 
    /// </summary>
    public bool IsEmpty()
    {
        return _inRunOut;
    }

    /// <summary>
    /// Returns true if bits can be read, including run-out bits
    /// </summary>
    public bool CanRead()
    {
        var avail = _original.Length - _original.Position;
        return avail > 0 || _runOutBits > 0;
    }

    /// <summary>
    /// Returns true if at least the given number of BYTES are available in the original data (i.e. excludes run-out)
    /// </summary>
    public bool HasHeadSpace(int byteSize)
    {
        var avail = _original.Length - _original.Position;
        return avail >= byteSize;
    }
}
using System;
using System.Collections.Generic;
using Enumerable = System.Linq.Enumerable;

namespace BitwiseSerialiser;

/// <summary>
/// Bit-wise and byte-wise access to a source of bytes
/// </summary>
internal class RunOutByteSource
{
    private readonly int _start;
    private readonly int _end;
    private readonly byte[] _data;
            
    /// <summary>
    /// Set to 'true' if more bytes were requested than supplied
    /// </summary>
    public bool WasOverRun { get; private set; }

    /// <summary> Last byte we popped when doing `NextBits` </summary>
    private byte _lastFrag;

    /// <summary> Offset in bits into byte list (caused when reading bits). Zero means aligned </summary>
    private int _offset;

    /// <summary>
    /// Index of next byte to read. If <c>_position &gt;= _data.Length</c>, then data is exhausted.
    /// </summary>
    private int _position;

    public RunOutByteSource(byte[] source, int start, int length)
    {
        _data = source;
        
        _start = start;
        _end = _start + Math.Min(_data.Length - _start, length);
        
        _lastFrag = 0;
        _offset = 0;
        _position = _start;
        WasOverRun = false;
    }

    /// <summary>
    /// Number of bytes remaining to be read
    /// </summary>
    private int RemainingCount() => _end - _position;

    /// <summary>
    /// Read byte at current position, and advance position
    /// </summary>
    private byte ReadNextByte() => _data[_position++];

    /// <summary>
    /// Read a non-byte aligned number of bits.
    /// Output is in the least-significant bits.
    /// Bit count must be 1..8.
    /// <para></para>
    /// Until `NextBits` is called with a value that
    /// re-aligns the feed, `NextByte` will run slower.
    /// </summary>
    public byte NextBits(int bitCount)
    {
        if (bitCount < 1) return 0;
        if (bitCount > 8) throw new Exception("Byte queue was asked for more than one byte");

        if (_offset == 0) // we are currently aligned
        {
            if (RemainingCount() < 1) // there is no more data
            {
                WasOverRun = true;
                return 0;
            }

            _lastFrag = ReadNextByte();
        }

        // simple case: there is enough data in the last frag
        int mask, shift;
        byte result;
        var rem = 8 - _offset;

        if (rem >= bitCount)
        {
            // example:
            // offset = 3, bit count = 3
            // 0 1 2 3 4 5 6 7
            // x x x ? ? ? _ _
            // next offset = 6
            // output = (last >> 2) & b00000111

            shift = rem - bitCount;
            mask = (1 << bitCount) - 1;
            result = (byte)((_lastFrag >> shift) & mask);
            _offset = (_offset + bitCount) % 8;
            return result;
        }

        // complex case: we need to mix data from two bytes

        // example:
        // offset = 3, bitCount = 7 => rem = 5, bitsFromNext = 2
        //
        // Input state:
        // 0 1 2 3 4 5 6 7 | 0 1 2 3 4 5 6 7
        // x x x A B C D E | F G _ _ _ _ _ _
        //
        // Output state:
        //     0 1 2 3 4 5 6 7
        // --> _ A B C D E F G 
        //
        // next offset = 2
        // output = ((last & b0001_1111) << 2) | ((next >> 6))
        if (RemainingCount() < 1) WasOverRun = true;
        var next = (RemainingCount() > 0) ? ReadNextByte() : (byte)0;

        mask = (1 << rem) - 1;
        var bitsFromNext = bitCount - rem;
        shift = 8 - bitsFromNext;

        result = (byte)(((_lastFrag & mask) << bitsFromNext) | (next >> shift));

        _lastFrag = next;
        _offset = bitsFromNext;
        return result;
    }

    /// <summary>
    /// Remove a byte from the queue. If there are no bytes
    /// available, a zero value is returned.
    /// </summary>
    public byte NextByte()
    {
        if (_offset != 0) return NextBits(8);
        if (RemainingCount() > 0) return ReadNextByte();

        // queue was empty
        WasOverRun = true;
        return 0;
    }

    public int GetRemainingLength() => RemainingCount();

    /// <summary>
    /// Record the current read position of the data source
    /// </summary>
    public Position GetPosition()
    {
        return new Position {
            BytePosition = _position,
            BitOffset = _offset,
            OverRun = WasOverRun,
            LastFrag = _lastFrag
        };
    }

    /// <summary>
    /// Restore a previously recorded position
    /// </summary>
    public void ResetTo(Position position)
    {
        if (position.BytePosition < _start) throw new ArgumentException("Position is out of range", nameof(position));
        if (position.BytePosition > _end) throw new ArgumentException("Position is out of range", nameof(position));
        
        _position = position.BytePosition;
        _offset = position.BitOffset;
        WasOverRun = position.OverRun;
        _lastFrag = position.LastFrag;
    }

    /// <summary>
    /// Position in the data source
    /// </summary>
    public class Position
    {
        internal int BytePosition { get; set; }
        internal int BitOffset { get; set; }
        internal bool OverRun { get; set; }
        internal byte LastFrag { get; set; }
    }
}
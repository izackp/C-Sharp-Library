using System;
using Mono;
using System.Text;
using CSharp_Library.Extensions;

//TODO: I maybe able to optimize by removing the casting, but I'm not gonna right now
public class DataReader {

#if COREFX
        static readonly Encoding encoding = Encoding.UTF8;
#else
    static readonly UTF8Encoding encoding = new UTF8Encoding();
#endif

    public byte[] Data;
    public int Position = 0;
    int _lastFlagBytePos = -1;
    int _flagCount = 0;

    DataConverter _converter;

    public int BytesAvailable {
        get { return (Data.Length - Position); }
    }

    public DataReader() {
        Data = new byte[] { };
        _converter = DataConverter.BigEndian;
    }

    public DataReader(byte[] content, DataConverter fromType = null) {
        LoadBytes(content, fromType);
    }

    #region Public methods
    public void LoadBytes(byte[] content, DataConverter fromType = null) {
        if (fromType == null)
            fromType = DataConverter.BigEndian;
        _converter = fromType;
        Data = content;
        Reset();
    }

    public void Reset() {
        Position = 0;
        _lastFlagBytePos = -1;
        _flagCount = 0;
    }

    public byte ReadByte() {
        return Read1();
    }

    public byte[] ReadBytes(int count) {
        Position += count;
        return Data.SubArray(Position - count, count);
    }
        
    public string ReadUTF() {
        int stringLength = (int)ReadVarInt();
        byte[] stringContentInByte = ReadBytes(stringLength);

        return encoding.GetString(stringContentInByte);
    }

    public int ReadInt() {
        return Read4();
    }

    public double ReadDouble() {
        return Read8();
    }

    public decimal ReadDecimal() {
        Position += 16;
        return _converter.GetDecimal(Data, Position - 16);
    }

    public short ReadShort() {
        return Read2();
    }

    /// <summary>
    /// Special Compression is used. A single byte is marked as a flag byte then filled with true/false bits.
    /// </summary>
    public bool ReadBoolean() {
        return ReadBit();
    }

    public char ReadChar() {
        return (char)Read2();
    }

    public long ReadLong() {
        return Read8();
    }

    public float ReadFloat() {
        return Read4();
    }

    public Single ReadSingle() {
        return Read4();
    }
    
    public uint ReadUInt() {
        return (uint)Read4();
    }

    public ushort ReadUShort() {
        return (ushort)Read2();
    }

    public ulong ReadULong() {
        return (ulong)Read8();
    }

    public sbyte ReadSByte() {
        return (sbyte)Read1();
    }

    public ushort ReadVarShort() {
        int resultVar = 0;
        for (int offset = 0; offset < 16; offset += 7) {
            byte readByte = Read1();
            bool hasContinuationFlag = (readByte & 128) == 128;
            int extractedValue = (readByte & 127);

            if (offset > 0)
                extractedValue = extractedValue << offset;

            resultVar += extractedValue;

            if (hasContinuationFlag == false)
                return (ushort)resultVar;
        }
        throw new Exception("Too much data");
    }

    public uint ReadVarInt() {
        uint resultVar = 0;
        for (int offset = 0; offset < 32; offset += 7) {
            byte readByte = Read1();
            bool hasContinuationFlag = (readByte & 128) == 128;
            uint extractedValue = (uint)(readByte & 127);

            if (offset > 0)
                extractedValue = extractedValue << offset;

            resultVar += extractedValue;

            if (hasContinuationFlag == false)
                return resultVar;
        }
        throw new Exception("Too much data");
    }

    public ulong ReadVarNum() {
        ulong resultVar = 0;
        for (int offset = 0; offset < 64; offset += 7) {
            byte readByte = Read1();
            bool hasContinuationFlag = (readByte & 128) == 128;
            ulong extractedValue = (ulong)(readByte & 127);

            if (offset > 0)
                extractedValue = extractedValue << offset;

            resultVar += extractedValue;

            if (hasContinuationFlag == false)
                return resultVar;
        }
        throw new Exception("Too much data");
    }

    #endregion

    #region Private methods

    bool ReadBit() {
        if (_lastFlagBytePos == -1 || _flagCount == 8) {
            _lastFlagBytePos = Position;
            Position += 1;
            _flagCount = 0;
        }
        byte flagByte = Data[_lastFlagBytePos];
        bool ret = (flagByte & (1 << _flagCount)) != 0;
        _flagCount += 1;
        return ret;
    }

    byte Read1() {
        Position += 1;
        return Data[Position - 1];
    }
    /*
    short Read2() {
        return (short)ReadVarNum();
    }

    int Read4() {
        return (int)ReadVarNum();
    }

    long Read8() {
        return (long)ReadVarNum();
    }
    */
    short Read2() {
        Position += 2;
        return _converter.GetInt16(Data, Position - 2);
    }

    int Read4() {
        Position += 4;
        return _converter.GetInt32(Data, Position - 4);
    }

    long Read8() {
        Position += 8;
        return _converter.GetInt64(Data, Position - 8);
    }
    #endregion
}

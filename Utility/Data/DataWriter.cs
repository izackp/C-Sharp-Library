using Mono;
using System;
using System.Text;
using CSharp_Library.Extensions;

public class DataWriter {

#if COREFX
        static readonly Encoding encoding = Encoding.UTF8;
#else
    static readonly UTF8Encoding encoding = new UTF8Encoding();
#endif

    #region Fields
    public byte[] Data = new byte[0xFFFFFF]; //around 16mb //TODO: Optimize
    public int Position = 0;
    public int _lastFlagBytePos = -1;
    public int _flagCount = 0;
    #endregion

    DataConverter _converter = DataConverter.BigEndian;

    #region Constructeurs
    public DataWriter() {
        _converter = DataConverter.BigEndian;
    }
    public DataWriter(DataConverter ToDataType) {
        _converter = ToDataType;
    }
    #endregion

    #region Public methods
    public void Reset(bool resetData = false) {
        Position = 0;
        _lastFlagBytePos = -1;
        _flagCount = 0;
        if (resetData)
            Data = new byte[0xFFFFFF];
    }

    public byte[] CopyBytes() {
        return Data.SubArray(0, Position);
    }

    public void WriteByte(byte byteToWrite) {
        Data[Position] = byteToWrite;
        Position += 1;
    }

    public void WriteBytes(byte[] bytesToWrite) {
        Array.Copy(bytesToWrite, 0, Data, Position, bytesToWrite.Length);
        Position += bytesToWrite.Length;
    }

    public void WriteUTF(string stringUTF8ToWrite) {
        byte[] stringContentInByte = encoding.GetBytes(stringUTF8ToWrite);
        int stringLength = stringContentInByte.Length;

        WriteVarInt((uint)stringLength);
        WriteBytes(stringContentInByte);
    }

    public void WriteInt(int intToWrite) {
        Write4(intToWrite);
    }

    public void WriteDouble(double doubleToWrite) {
        Write8((long)doubleToWrite);
    }

    public void WriteDecimal(decimal decToWrite) {
        _converter.PutBytes(Data, Position, decToWrite);
        Position += 16;
    }

    public void WriteShort(short shortToWrite) {
        Write2(shortToWrite);
    }

    /// <summary>
    /// Special Compression is used. A single byte will be marked as a flag byte then filled with true/false bits.
    /// </summary>
    /// <param name="boolToWrite"></param>
    public void WriteBoolean(bool boolToWrite) {
        WriteBit(boolToWrite);
    }

    public void WriteChar(char charToWrite) {
        Write2((short)charToWrite);
    }

    public void WriteLong(long longToWrite) {
        Write8(longToWrite);
    }

    public void WriteFloat(float floatToWrite) {
        Write4((int)floatToWrite);
    }

    public void WriteSingle(Single singleToWrite) {
        Write4((int)singleToWrite);
    }

    public void WriteUInt(uint uintToWrite) {
        Write4((int)uintToWrite);
    }

    public void WriteUShort(ushort ushortToWrite) {
        Write2((short)ushortToWrite);
    }

    public void WriteULong(ulong ulongToWrite) {
        Write8((long)ulongToWrite);
    }

    public void WriteSByte(sbyte sbyteToWrite) {
        WriteByte((byte)sbyteToWrite);
    }

    public void WriteVarShort(ushort value) {
        if ((value & 0xFF80) == 0) {
            WriteByte((byte)value);
            return;
        }
        int valueToWrite = value;
        while (valueToWrite != 0) {
            byte desiredValue = (byte)(valueToWrite & 127);
            valueToWrite = valueToWrite >> 7;
            if (valueToWrite > 0)
                desiredValue = (byte)(desiredValue | 128);
            WriteByte(desiredValue);
        }
    }

    public void WriteVarInt(uint value) {
        if ((value & 0xFFFFFF80) == 0) {
            WriteByte((byte)value);
            return;
        }
        uint valueToWrite = value;
        while (valueToWrite != 0) {
            byte desiredValue = (byte)(valueToWrite & 127);
            valueToWrite = valueToWrite >> 7;
            if (valueToWrite > 0)
                desiredValue = (byte)(desiredValue | 128);
            WriteByte(desiredValue);
        }
    }

    public void WriteVarNum(ulong value) {
        if ((value & 0xFFFFFFFFFFFFFF80) == 0) {
            WriteByte((byte)value);
            return;
        }
        ulong valueToWrite = value;
        while (valueToWrite != 0) {
            byte desiredValue = (byte)(valueToWrite & 127);
            valueToWrite = valueToWrite >> 7;
            if (valueToWrite > 0)
                desiredValue = (byte)(desiredValue | 128);
            WriteByte(desiredValue);
        }
    }
    #endregion

    #region Private methods

    void WriteBit(bool bit) {
        if (_lastFlagBytePos == -1 || _flagCount == 8) {
            _lastFlagBytePos = Position;
            Position += 1;
            _flagCount = 0;
        }
        if (bit) {
            byte flagByte = Data[_lastFlagBytePos];
            Data[_lastFlagBytePos] = (byte)(flagByte | (1 << _flagCount));
        }
        _flagCount += 1;
    }
    /*
    void Write2(short shortToWrite) {
        WriteVarNum((ulong)shortToWrite);
    }

    void Write4(int intToWrite) {
        WriteVarNum((ulong)intToWrite);
    }

    void Write8(long longToWrite) {
        WriteVarNum((ulong)longToWrite);
    }
    */

    void Write2(short shortToWrite) {
        _converter.PutBytes(Data, Position, shortToWrite);
        Position += 2;
    }

    void Write4(int intToWrite) {
        _converter.PutBytes(Data, Position, intToWrite);
        Position += 4;
    }

    void Write8(long longToWrite) {
        _converter.PutBytes(Data, Position, longToWrite);
        Position += 8;
    }
    #endregion
}

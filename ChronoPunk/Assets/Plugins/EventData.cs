using System;
using System.Runtime.InteropServices;

namespace Telemetry
{
public enum AttributeType : uint
{
    Bool,
    Int32,
    Int64,
    Float,
    Double,
    FixedStr
}

// 8 bytes: 7 chars + '\n'.
[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 8)]
public struct FixedString8
{
    public byte b0;
    public byte b1;
    public byte b2;
    public byte b3;
    public byte b4;
    public byte b5;
    public byte b6;
    public byte b7;
}

[StructLayout(LayoutKind.Explicit)]
public struct AttributeValue
{
    [FieldOffset(0)] public byte b;
    [FieldOffset(0)] public int i32;
    [FieldOffset(0)] public long i64;
    [FieldOffset(0)] public float f;
    [FieldOffset(0)] public double d;
    [FieldOffset(0)] public FixedString8 FixedStr;
}

[StructLayout(LayoutKind.Sequential, Pack = 8)]
public struct EventAttributeData
{
    public int attributeNameID;
    public AttributeType attributeTypeID;
    public AttributeValue value;

    public int getSize()
    {
        int size = 0;

        size += Marshal.SizeOf(typeof(int));
        size += Marshal.SizeOf(typeof(AttributeType));
        size += Marshal.SizeOf(typeof(AttributeValue));
        
        return size;
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 8)]
public struct EventData
{
    public int eventTypeID;
    public long timestamp;

    public IntPtr attributes;
    public int attributeCount;
}

}
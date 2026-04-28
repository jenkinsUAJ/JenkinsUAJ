using System;
using UnityEngine;
using System.Runtime.InteropServices;

namespace Telemetry
{
    public static class TelemetryUtils
    {
        private static readonly int EventTypeOffset = (int)Marshal.OffsetOf<EventData>(nameof(EventData.eventTypeID));
        private static readonly int EventTimestampOffset = (int)Marshal.OffsetOf<EventData>(nameof(EventData.timestamp));
        private static readonly int EventAttributesOffset = (int)Marshal.OffsetOf<EventData>(nameof(EventData.attributes));
        private static readonly int EventAttributeCountOffset = (int)Marshal.OffsetOf<EventData>(nameof(EventData.attributeCount));
        private static readonly int EventAttributeSize = Marshal.SizeOf<EventAttributeData>();

        private static readonly int AttributeNameOffset = (int)Marshal.OffsetOf<EventAttributeData>(nameof(EventAttributeData.attributeNameID));
        private static readonly int AttributeTypeOffset = (int)Marshal.OffsetOf<EventAttributeData>(nameof(EventAttributeData.attributeTypeID));
        private static readonly int AttributeValueOffset = (int)Marshal.OffsetOf<EventAttributeData>(nameof(EventAttributeData.value));

        public static IntPtr GetEventAttributesPtr(IntPtr eventPtr)
        {
            return Marshal.ReadIntPtr(eventPtr, EventAttributesOffset);
        }

        public static int GetEventAttributeCount(IntPtr eventPtr)
        {
            return Marshal.ReadInt32(eventPtr, EventAttributeCountOffset);
        }

        public static void WriteEventHeader(IntPtr eventPtr, int eventTypeId, long timestamp)
        {
            if (eventPtr == IntPtr.Zero)
            {
                return;
            }

            Marshal.WriteInt32(eventPtr, EventTypeOffset, eventTypeId);
            Marshal.WriteInt64(eventPtr, EventTimestampOffset, timestamp);
        }

        public static void WriteFixedStr8(IntPtr destination, string str)
        {
            if (destination == IntPtr.Zero)
            {
                return;
            }

            for (int index = 0; index < 8; index++)
            {
                Marshal.WriteByte(destination, index, 0);
            }

            int newlineIndex = 7;

            if (!string.IsNullOrEmpty(str))
            {
                int inspectCount = Math.Min(str.Length, 8);

                for (int index = 0; index < inspectCount; index++)
                {
                    char ch = str[index];

                    if (ch == '\n')
                    {
                        newlineIndex = index;
                        break;
                    }

                    if (index < 7)
                    {
                        Marshal.WriteByte(destination, index, ch <= 0x7F ? (byte)ch : (byte)'?');
                    }
                }
            }

            Marshal.WriteByte(destination, newlineIndex, (byte)'\n');
        }

        public static void WriteAttributeBool(IntPtr attributesBase, int index, int attributeNameId, bool value)
        {
            IntPtr destination = GetAttributePtr(attributesBase, index);
            WriteHeader(destination, attributeNameId, AttributeType.Bool);
            Marshal.WriteByte(destination, AttributeValueOffset, value ? (byte)1 : (byte)0);
        }

        public static void WriteAttributeInt32(IntPtr attributesBase, int index, int attributeNameId, int value)
        {
            IntPtr destination = GetAttributePtr(attributesBase, index);
            WriteHeader(destination, attributeNameId, AttributeType.Int32);
            Marshal.WriteInt32(destination, AttributeValueOffset, value);
        }

        public static void WriteAttributeInt64(IntPtr attributesBase, int index, int attributeNameId, long value)
        {
            IntPtr destination = GetAttributePtr(attributesBase, index);
            WriteHeader(destination, attributeNameId, AttributeType.Int64);
            Marshal.WriteInt64(destination, AttributeValueOffset, value);
        }

        public static void WriteAttributeFloat(IntPtr attributesBase, int index, int attributeNameId, float value)
        {
            IntPtr destination = GetAttributePtr(attributesBase, index);
            WriteHeader(destination, attributeNameId, AttributeType.Float);
            Marshal.WriteInt32(destination, AttributeValueOffset, BitConverter.SingleToInt32Bits(value));
        }

        public static void WriteAttributeDouble(IntPtr attributesBase, int index, int attributeNameId, double value)
        {
            IntPtr destination = GetAttributePtr(attributesBase, index);
            WriteHeader(destination, attributeNameId, AttributeType.Double);
            Marshal.WriteInt64(destination, AttributeValueOffset, BitConverter.DoubleToInt64Bits(value));
        }

        public static void WriteAttributeFixedStr(IntPtr attributesBase, int index, int attributeNameId, string value)
        {
            IntPtr destination = GetAttributePtr(attributesBase, index);
            WriteHeader(destination, attributeNameId, AttributeType.FixedStr);
            IntPtr valuePtr = IntPtr.Add(destination, AttributeValueOffset);
            WriteFixedStr8(valuePtr, value);
        }

        private static IntPtr GetAttributePtr(IntPtr attributesBase, int index)
        {
            return IntPtr.Add(attributesBase, index * EventAttributeSize);
        }

        private static void WriteHeader(IntPtr destination, int attributeNameId, AttributeType type)
        {
            Marshal.WriteInt32(destination, AttributeNameOffset, attributeNameId);
            Marshal.WriteInt32(destination, AttributeTypeOffset, (int)type);
        }

        public static int GetUserID()
        {
            if (PlayerPrefs.HasKey("USER_ID"))
            {
                return PlayerPrefs.GetInt("USER_ID");
            }
            else
            {
                int id = SystemInfo.deviceUniqueIdentifier.GetHashCode();
                PlayerPrefs.SetInt("USER_ID", id);
                return id;
            }
        }
    }

}
﻿using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Reflection;

namespace x86Emulator
{
   public static class Util
    {
        public static int CountSet(this BitArray bits)
        {
            int count = 0;

            for (int i = 0; i < bits.Length; i++)
            {
                if (bits[i])
                    count++;
            }

            return count;
        }

        public static byte GetLow(this byte b)
        {
            return (byte)(b & 0x0f);
        }

        public static byte SetLow(this byte b, byte value)
        {
            return (byte)((b & 0xf0) + (value & 0x0f));
        }

        public static byte SetHigh(this byte b, byte value)
        {
            return (byte)((value.GetLow() << 4) + b.GetLow());
        }

        public static byte GetHigh(this byte b)
        {
            return (byte)((b >> 4) & 0x0f);
        }

        public static ushort GetLow(this ushort b)
        {
            return (ushort)(b & 0x00ff);
        }

        public static ushort SetLow(this ushort b, ushort value)
        {
            return (byte)((b & 0xff00) + (value & 0x00ff));
        }

        public static ushort SetHigh(this ushort b, ushort value)
        {
            return (ushort)((value.GetLow() << 8) + b.GetLow());
        }

        public static ushort GetHigh(this ushort b)
        {
            return (ushort)((b >> 8) & 0x00ff);
        }

        public static byte ToBCD(int value)
        {
            int tens = value / 10;
            int ones = value % 10;

            var ret = (byte)(((byte)tens << 4) + (byte)ones);

            return ret;
        }

        public static uint ToUInt32BigEndian(byte[] buffer, int offset)
        {
            uint val = (uint)(((buffer[offset + 0] << 24) & 0xFF000000U) | ((buffer[offset + 1] << 16) & 0x00FF0000U)
                | ((buffer[offset + 2] << 8) & 0x0000FF00U) | ((buffer[offset + 3] << 0) & 0x000000FFU));
            return val;
        }
        public static ulong ToUInt64BigEndian(byte[] buffer, int offset)
        {
            return (((ulong)ToUInt32BigEndian(buffer, offset + 0)) << 32) | ToUInt32BigEndian(buffer, offset + 4);
        }

        public static T ByteArrayToStructure<T>(byte[] bytes) where T : struct
        {
            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);

            T structure = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            handle.Free();

            return structure;
        }

        public static T ByteArrayToStructureBigEndian<T>(byte[] bytes) where T : struct
        {
            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);

            T structure = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            handle.Free();
            System.Type t = structure.GetType();
            FieldInfo[] fieldInfo = t.GetFields();
            foreach (FieldInfo fi in fieldInfo)
            {
                if (fi.FieldType == typeof(System.Int16))
                {
                    // TODO
                }
                else if (fi.FieldType == typeof(System.Int32))
                {
                    // TODO
                }
                else if (fi.FieldType == typeof(System.Int64))
                {
                    // TODO
                }
                else if (fi.FieldType == typeof(System.UInt16))
                {
                    UInt16 num = (UInt16)fi.GetValue(structure);
                    byte[] tmp = BitConverter.GetBytes(num);
                    Array.Reverse(tmp);
                    fi.SetValue(structure, BitConverter.ToUInt16(tmp, 0));
                }
                else if (fi.FieldType == typeof(System.UInt32))
                {
                    UInt32 num = (UInt32)fi.GetValue(structure);
                    byte[] tmp = BitConverter.GetBytes(num);
                    Array.Reverse(tmp);
                    fi.SetValue(structure, BitConverter.ToUInt32(tmp, 0));
                }
                else if (fi.FieldType == typeof(System.UInt64))
                {
                    UInt64 num = (UInt64)fi.GetValue(structure);
                    byte[] tmp = BitConverter.GetBytes(num);
                    Array.Reverse(tmp);
                    fi.SetValue(structure, BitConverter.ToUInt64(tmp, 0));
                }
            }
            return structure;
        }

        public static uint SwapByteOrder(uint source)
        {
            uint dest;

            dest = (uint)(((source & 0xff) << 24) | 
                ((source & 0xff00) << 8) | 
                ((source & 0xff0000) >> 8) | 
                ((source & 0xff000000) >> 24));

            return dest;
        }

        public static ushort SwapByteOrder(ushort source)
        {
            ushort dest;

            dest = (ushort)(((byte)(source << 8)) | ((byte)(source >> 8)));

            return dest;
        }

        public static void ByteArrayToUShort(byte[] source, ushort[] dest, int index, bool bswap)
        {
            for (int i = 0, j = index; i < source.Length; i += 2, j++)
            {
                if(bswap)
                    dest[j] = (ushort)((source[i] << 8) + source[i + 1]);
                else
                    dest[j] = (ushort)((source[i + 1] << 8) + source[i]);
            }
        }

        public static void ByteArrayToUShort(byte[] source, ushort[] dest, int index)
        {
            Util.ByteArrayToUShort(source, dest, index, false);
        }

        public static void UShortArrayToByte(ushort[] source, byte[] dest, int index)
        {
            for (int i = 0, j = index; i < dest.Length; i += 2, j++)
            {
                dest[i] = (byte)source[j];
                dest[i + 1] = (byte)(source[j] >> 8);
            }
        }
    }
}

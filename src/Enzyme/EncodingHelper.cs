using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Enzyme
{
    static class EncodingHelper
    {
        public static readonly UTF8Encoding Utf8 = new UTF8Encoding(false);

        public static unsafe short WriteStringUnsafe(byte* writeTo, string value)
        {
            fixed (char* ptr = value)
            {
                checked
                {
                    return (short)Utf8.GetBytes(ptr, value.Length, writeTo, int.MaxValue);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ZigInt(int value) => (uint)((value << 1) ^ (value >> 31));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ZigLong(long value) => (ulong)((value << 1) ^ (value >> 63));

        const long Int64Msb = ((long)1) << 63;
        const int Int32Msb = ((int)1) << 31;

        public static int ZagToInt(uint ziggedValue)
        {
            var value = (int)ziggedValue;
            return (-(value & 0x01)) ^ ((value >> 1) & ~Int32Msb);
        }

        public static long ZagToLong(ulong ziggedValue)
        {
            var value = (long)ziggedValue;
            return (-(value & 0x01L)) ^ ((value >> 1) & ~Int64Msb);
        }

        public static unsafe short WriteUInt64Variant(byte* writeTo, ulong value)
        {
            var count = 0;
            do
            {
                writeTo[count++] = (byte)((value & 0x7F) | 0x80);
            } while ((value >>= 7) != 0);

            writeTo[count - 1] &= 0x7F;
            return (short)count;
        }

        public static unsafe short WriteUInt32Variant(byte* writeTo, uint value)
        {
            var count = 0;
            do
            {
                writeTo[count++] = (byte)((value & 0x7F) | 0x80);
            } while ((value >>= 7) != 0);

            writeTo[count - 1] &= 0x7F;
            return (short)count;
        }

        public static unsafe int TryReadUInt32VariantWithoutMoving(byte* payload, out uint value)
        {
            var readPos = 0;
            value = payload[readPos++];
            if ((value & 0x80) == 0)
            {
                return 1;
            }
            value &= 0x7F;

            uint chunk = payload[readPos++];
            value |= (chunk & 0x7F) << 7;
            if ((chunk & 0x80) == 0)
            {
                return 2;
            }

            chunk = payload[readPos++];
            value |= (chunk & 0x7F) << 14;
            if ((chunk & 0x80) == 0)
            {
                return 3;
            }

            chunk = payload[readPos++];
            value |= (chunk & 0x7F) << 21;
            if ((chunk & 0x80) == 0)
            {
                return 4;
            }

            chunk = payload[readPos];
            value |= chunk << 28; // can only use 4 bits from this chunk
            if ((chunk & 0xF0) == 0)
            {
                return 5;
            }

            if ((chunk & 0xF0) == 0xF0
                && payload[++readPos] == 0xFF
                && payload[++readPos] == 0xFF
                && payload[++readPos] == 0xFF
                && payload[++readPos] == 0xFF
                && payload[++readPos] == 0x01)
            {
                return 10;
            }

            throw new OverflowException();
        }

        public static unsafe int TryReadUInt64VariantWithoutMoving(byte* payload, out ulong value)
        {
            var readPos = 0;
            value = payload[readPos++];
            if ((value & 0x80) == 0)
            {
                return 1;
            }
            value &= 0x7F;

            ulong chunk = payload[readPos++];
            value |= (chunk & 0x7F) << 7;
            if ((chunk & 0x80) == 0)
            {
                return 2;
            }

            chunk = payload[readPos++];
            value |= (chunk & 0x7F) << 14;
            if ((chunk & 0x80) == 0)
            {
                return 3;
            }

            chunk = payload[readPos++];
            value |= (chunk & 0x7F) << 21;
            if ((chunk & 0x80) == 0)
            {
                return 4;
            }

            chunk = payload[readPos++];
            value |= (chunk & 0x7F) << 28;
            if ((chunk & 0x80) == 0)
            {
                return 5;
            }

            chunk = payload[readPos++];
            value |= (chunk & 0x7F) << 35;
            if ((chunk & 0x80) == 0)
            {
                return 6;
            }

            chunk = payload[readPos++];
            value |= (chunk & 0x7F) << 42;
            if ((chunk & 0x80) == 0)
            {
                return 7;
            }


            chunk = payload[readPos++];
            value |= (chunk & 0x7F) << 49;
            if ((chunk & 0x80) == 0)
            {
                return 8;
            }

            chunk = payload[readPos++];
            value |= (chunk & 0x7F) << 56;
            if ((chunk & 0x80) == 0)
            {
                return 9;
            }

            chunk = payload[readPos];
            value |= chunk << 63; // can only use 1 bit from this chunk

            if ((chunk & ~(ulong)0x01) != 0)
            {
                throw new OverflowException();
            }
            return 10;
        }
    }
}
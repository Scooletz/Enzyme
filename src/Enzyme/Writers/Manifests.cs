namespace Enzyme.Writers
{
    static class Manifests
    {
        // public const byte Reserved = 0;

        public const byte BoolType = 1;

        public const byte ByteType = 2;
        public const byte SByteType = 3;

        public const byte Int16Type = 4;
        public const byte UInt16Type = 5;

        public const byte Int32Type = 6;
        public const byte UInt32Type = 7;

        public const byte Int64Type = 8;
        public const byte UInt64Type = 9;

        public const byte StringType = 10;
        public const byte GuidType = 11;

        public const byte DateTime = 12;
        public const byte Object = 13;

        public const byte Array = 16;
        public const byte Nullable = 32;

        public static bool IsArray(byte manifest) => (manifest & Array) > 0;
        public static bool IsObject(byte manifest) => manifest == Object;
        public static bool IsPrimitive(byte manifest) => manifest < Object;
    }
}
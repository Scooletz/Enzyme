using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Enzyme.Writers;
using NUnit.Framework;

namespace Enzyme.Tests
{
    public class SerializationTests
    {
        /// <summary>
        /// First field number
        /// </summary>
        const byte F1 = 17;

        [Test]
        public void Empty()
        {
            var v = new object();
            var ctx = new object();

            var writer = new AssertingWriter(ctx);

            Serializer.Serialize(ref v, ref ctx, writer);
        }

        [TestCase(true, new byte[] { F1, Manifests.BoolType, 1 })]
        [TestCase(false, new byte[] { F1, Manifests.BoolType, 0 })]
        public void Bool(bool value, byte[] expected)
        {
            Run(value, expected);
        }

        [TestCase(new[] { true }, new byte[] { F1, Manifests.BoolType | Manifests.Array, 1, 0, 1 })]
        [TestCase(new[] { false }, new byte[] { F1, Manifests.BoolType | Manifests.Array, 1, 0, 0 })]
        [TestCase(new[] { true, false, true, false }, new byte[] { F1, Manifests.BoolType | Manifests.Array, 4, 0, 1, 0, 1, 0 })]
        public void Bools(bool[] value, byte[] expected)
        {
            Run(value, expected);
        }

        [Test]
        public void BoolNullable_True()
        {
            Run((bool?)true, new byte[] { F1, Manifests.BoolType | Manifests.Nullable, 1 });
        }

        [Test]
        public void BoolNullable_False()
        {
            Run((bool?)false, new byte[] { F1, Manifests.BoolType | Manifests.Nullable, 0 });
        }

        [Test]
        public void BoolNullable_NoValue()
        {
            Run(default(bool?), new byte[] { });
        }

        [TestCase((sbyte)0, new byte[] { F1, Manifests.SByteType, 0 })]
        [TestCase((sbyte)1, new byte[] { F1, Manifests.SByteType, 1 })]
        [TestCase((sbyte)2, new byte[] { F1, Manifests.SByteType, 2 })]
        [TestCase(sbyte.MinValue, new byte[] { F1, Manifests.SByteType, 128 })]
        [TestCase(sbyte.MaxValue, new byte[] { F1, Manifests.SByteType, 127 })]
        public void SByte(sbyte value, byte[] expected)
        {
            Run(value, expected);
        }

        [TestCase((byte)0, new byte[] { F1, Manifests.ByteType, 0 })]
        [TestCase((byte)1, new byte[] { F1, Manifests.ByteType, 1 })]
        [TestCase((byte)2, new byte[] { F1, Manifests.ByteType, 2 })]
        [TestCase((byte)255, new byte[] { F1, Manifests.ByteType, 255 })]
        public void Byte(byte value, byte[] expected)
        {
            Run(value, expected);
        }

        [TestCase(new[] { (byte)0 }, new byte[] { F1, Manifests.ByteType | Manifests.Array, 1, 0, 0 })]
        [TestCase(new[] { (byte)3 }, new byte[] { F1, Manifests.ByteType | Manifests.Array, 1, 0, 3 })]
        public void Bytes(byte[] value, byte[] expected)
        {
            Run(value, expected);
        }

        [TestCase((short)0, new byte[] { F1, Manifests.Int16Type, 0, 0 })]
        [TestCase((short)1, new byte[] { F1, Manifests.Int16Type, 1, 0 })]
        [TestCase((short)2, new byte[] { F1, Manifests.Int16Type, 2, 0 })]
        [TestCase(short.MinValue, new byte[] { F1, Manifests.Int16Type, 0, 128 })]
        [TestCase(short.MaxValue, new byte[] { F1, Manifests.Int16Type, 255, 127 })]
        public void Short(short value, byte[] expected)
        {
            Run(value, expected);
        }

        [TestCase((ushort)0, new byte[] { F1, Manifests.UInt16Type, 0, 0 })]
        [TestCase((ushort)1, new byte[] { F1, Manifests.UInt16Type, 1, 0 })]
        [TestCase((ushort)2, new byte[] { F1, Manifests.UInt16Type, 2, 0 })]
        [TestCase(ushort.MaxValue, new byte[] { F1, Manifests.UInt16Type, 255, 255 })]
        public void UShort(ushort value, byte[] expected)
        {
            Run(value, expected);
        }

        [TestCase(0, new byte[] { F1, Manifests.Int32Type, 0 })]
        [TestCase(1, new byte[] { F1, Manifests.Int32Type, 2 })]
        [TestCase(2, new byte[] { F1, Manifests.Int32Type, 4 })]
        [TestCase(-1, new byte[] { F1, Manifests.Int32Type, 1 })]
        [TestCase(-2, new byte[] { F1, Manifests.Int32Type, 3 })]
        [TestCase(256, new byte[] { F1, Manifests.Int32Type, 128, 4 })]
        public void Int(int value, byte[] expected)
        {
            Run(value, expected);
        }

        [Test]
        public void Int_Nullable()
        {
            Run(default(int?), new byte[] { });
        }

        [Test]
        public void Int_Nullable_1()
        {
            Run((int?)1, new byte[] { F1, Manifests.Int32Type | Manifests.Nullable, 2 });
        }

        [TestCase(new[] { 0 }, new byte[] { F1, Manifests.Int32Type | Manifests.Array, 1, 0, 0 })]
        [TestCase(new[] { 256 }, new byte[] { F1, Manifests.Int32Type | Manifests.Array, 2, 0, 128, 4 })]
        public void Ints(int[] value, byte[] expected)
        {
            Run(value, expected);
        }

        [Test]
        public void Ints_Nullable_256()
        {
            var value = new int?[] { 256 };
            var expected = new byte[] { F1, Manifests.Int32Type | Manifests.Array | Manifests.Nullable, 3, 0, 1, 128, 4 };

            Run(value, expected);
        }

        [Test]
        public void Ints_Nullable()
        {
            var value = new int?[] { null };
            var expected = new byte[] { F1, Manifests.Int32Type | Manifests.Array | Manifests.Nullable, 1, 0, 0 };

            Run(value, expected);
        }

        [TestCase((uint)0, new byte[] { F1, Manifests.UInt32Type, 0 })]
        [TestCase((uint)1, new byte[] { F1, Manifests.UInt32Type, 1 })]
        [TestCase((uint)2, new byte[] { F1, Manifests.UInt32Type, 2 })]
        [TestCase((uint)256, new byte[] { F1, Manifests.UInt32Type, 128, 2 })]
        public void UInt(uint value, byte[] expected)
        {
            Run(value, expected);
        }

        [TestCase(0L, new byte[] { F1, Manifests.Int64Type, 0 })]
        [TestCase(1L, new byte[] { F1, Manifests.Int64Type, 2 })]
        [TestCase(2L, new byte[] { F1, Manifests.Int64Type, 4 })]
        [TestCase(-1L, new byte[] { F1, Manifests.Int64Type, 1 })]
        [TestCase(-2L, new byte[] { F1, Manifests.Int64Type, 3 })]
        [TestCase(256L, new byte[] { F1, Manifests.Int64Type, 128, 4 })]
        [TestCase(long.MaxValue, new byte[] { F1, Manifests.Int64Type, 254, 255, 255, 255, 255, 255, 255, 255, 255, 1 })]
        [TestCase(long.MinValue, new byte[] { F1, Manifests.Int64Type, 255, 255, 255, 255, 255, 255, 255, 255, 255, 1 })]
        public void Long(long value, byte[] expected)
        {
            Run(value, expected);
        }

        [TestCase((ulong)0, new byte[] { F1, Manifests.UInt64Type, 0 })]
        [TestCase((ulong)1, new byte[] { F1, Manifests.UInt64Type, 1 })]
        [TestCase((ulong)2, new byte[] { F1, Manifests.UInt64Type, 2 })]
        [TestCase((ulong)256, new byte[] { F1, Manifests.UInt64Type, 128, 2 })]
        [TestCase(ulong.MaxValue, new byte[] { F1, Manifests.UInt64Type, 255, 255, 255, 255, 255, 255, 255, 255, 255, 1 })]
        public void ULong(ulong value, byte[] expected)
        {
            Run(value, expected);
        }

        [Test]
        public void Guid()
        {
            var manifest = new[] { F1, Manifests.GuidType };

            var value = new Guid(0x0403_0201, 0x0605, 0x0807, 9, 10, 11, 12, 13, 14, 15, 16);
            var expected = manifest.Concat(Enumerable.Range(1, 16).Select(i => (byte)i).ToArray()).ToArray();

            Run(value, expected);
        }

        [Test]
        public void Guid_Nullable()
        {
            var expected = new byte[] { };

            var value = default(Guid?);
            Run(value, expected);
        }

        [Test]
        public void DateTime()
        {
            var manifest = new[] { F1, Manifests.DateTime };

            var value = System.DateTime.UtcNow;
            var ticks = value.Ticks;
            var expected = manifest.Concat(BitConverter.GetBytes(ticks)).ToArray();

            Run(value, expected);
        }

        [Test]
        public void String()
        {
            var manifest = new[] { F1, Manifests.StringType };
            const string value = "test";

            var expected = manifest.Concat(GetExpected(value)).ToArray();

            Run(value, expected);
        }

        [Test]
        public void String_Null()
        {
            var expected = new byte[] { };
            const string value = null;

            Run(value, expected);
        }

        [Test]
        public void Strings()
        {
            var manifest = new byte[] { F1, Manifests.StringType | Manifests.Array };
            const string str = "test";

            var value = new[] { str, str };

            var bytes = GetExpected(str);
            var valuesBytes = bytes.Concat(bytes);
            var expected = manifest.Concat(PrefixShortLength(valuesBytes)).ToArray();

            Run(value, expected);
        }

        [Test]
        public void Strings_vlong()
        {
            var manifest = new byte[] { F1, Manifests.StringType | Manifests.Array };
            const string str = "test3452347fh3h4f89h349fh9h3489f8342fh43f98h348fh8348f9h4383h42f";

            var value = new[] { str, str };
            var bytes = GetExpected(str);
            var valuesBytes = bytes.Concat(bytes).ToArray();
            var expected = manifest.Concat(PrefixShortLength(valuesBytes)).ToArray();

            Run(value, expected);
        }

        [Test]
        public void Strings_Empty()
        {
            var manifest = new byte[] { F1, Manifests.StringType | Manifests.Array, };
            const string str = "";

            var value = new[] { str, str };

            var bytes = GetExpected(str);
            var valuesBytes = bytes.Concat(bytes);
            var expected = manifest.Concat(PrefixShortLength(valuesBytes)).ToArray();

            Run(value, expected);
        }

        [Test]
        public void Strings_Null()
        {
            var expected = new byte[] { F1, Manifests.StringType | Manifests.Array, 4, 0, 255, 255, 255, 255 };
            const string str = null;

            var value = new[] { str, str };

            Run(value, expected);
        }

        [Test]
        public void String_Empty()
        {
            var manifest = new[] { F1, Manifests.StringType };
            const string value = "";

            var expected = manifest.Concat(GetExpected(value)).ToArray();

            Run(value, expected);
        }

        [Test]
        public void Enumerable_ByVal()
        {
            var value = new List<int> { 0 };
            var expected = new byte[] { F1, Manifests.Int32Type | Manifests.Array, 1, 0, 0 };

            Run(value, expected);
        }

        [Test]
        public void Enumerable_ByRefs()
        {
            var value = new List<int?> { null };
            var expected = new byte[] { F1, Manifests.Int32Type | Manifests.Array | Manifests.Nullable, 1, 0, 0 };

            Run(value, expected);
        }

        [Test]
        public void Memory_ByVal()
        {
            var value = new Memory<int>(new[] { 0 });
            var expected = new byte[] { F1, Manifests.Int32Type | Manifests.Array, 1, 0, 0 };

            Run(value, expected);
        }

        [Test]
        public void Memory_ByRef()
        {
            var value = new Memory<int?>(new int?[] { null });
            var expected = new byte[] { F1, Manifests.Int32Type | Manifests.Array | Manifests.Nullable, 1, 0, 0 };

            Run(value, expected);
        }

        [Test]
        public void Memory_Bytes()
        {
            var value = new Memory<byte>(new[] { (byte)0 });
            var expected = new byte[] { F1, Manifests.ByteType | Manifests.Array, 1, 0, 0 };

            Run(value, expected);
        }

        [Test]
        public void Noop()
        {
            var value = new object();
            var expected = new byte[] { };

            Run(value, expected);
        }

        [Test]
        public void Class()
        {
            var v = new OneFieldClass<bool>(true);
            var ctx = new object();

            var writer = new AssertingWriter(ctx, F1, Manifests.BoolType, 1);

            Serializer.Serialize(ref v, ref ctx, writer);
        }

        [Test]
        public void Complex()
        {
            var guid = new Guid("35ED53A9-A0C2-4C42-A4FA-CDB0A5E8E51B");
            var value = new Value
            {
                Bool = true,
                Int = 2,
                String = "some",
                NullBool = true,
                Id = guid
            };

            var g = guid.ToByteArray();

            var ctx = new object();

            Serializer.Serialize(ref value, ref ctx, new AssertingWriter(ctx,
                1, Manifests.BoolType, 1,
                2, Manifests.Int32Type, 4,
                3, Manifests.StringType, 4, 0, (byte)'s', (byte)'o', (byte)'m', (byte)'e',
                4, Manifests.Nullable | Manifests.BoolType, 1,
                5, Manifests.GuidType, g[0], g[1], g[2], g[3], g[4], g[5], g[6], g[7], g[8], g[9], g[10], g[11], g[12], g[13], g[14], g[15]
                ));
        }

        [Test]
        public void Nested_non_generic()
        {
            var value = new A
            {
                B = new B
                {
                    Int = 3
                }
            };

            var ctx = new object();

            Serializer.Serialize(ref value, ref ctx, new AssertingWriter(ctx,
                A.Order, Manifests.Object, 3, 0,
                    B.Order, Manifests.Int32Type, 6
            ));
        }

        [Test]
        public void Nested_generic()
        {
            var value =
                new Nested<Nested<int>>()
                {
                    Value = 1,
                    Tail =
                        new Nested<int>
                        {
                            Value = 2,
                            Tail = 3,
                        }
                };

            var ctx = new object();

            Serializer.Serialize(ref value, ref ctx, new AssertingWriter(ctx,
                1, Manifests.Int32Type, 2,
                2, Manifests.Object, 6, 0,
                    1, Manifests.Int32Type, 4,
                    2, Manifests.Int32Type, 6
            ));
        }

        [DataContract]
        public class Value
        {
            [DataMember(Order = 1)] public bool Bool;
            [DataMember(Order = 2)] public int Int;
            [DataMember(Order = 3)] public string String;
            [DataMember(Order = 4)] public bool? NullBool;
            [DataMember(Order = 5)] public Guid Id;
        }

        [DataContract]
        public struct A
        {
            public const byte Order = 3;
            [DataMember(Order = Order)] public B B;
        }

        [DataContract]
        public struct B
        {
            public const byte Order = 5;
            [DataMember(Order = Order)] public int Int;
        }

        [DataContract]
        public struct Nested<TTail>
        {
            [DataMember(Order = 1)]
            public int Value;

            [DataMember(Order = 2)]
            public TTail Tail;
        }

        static void Run<T>(T value, byte[] expected)
        {
            var v = new OneFieldStruct<T>(value);
            var ctx = new object();

            var writer = new AssertingWriter(ctx, expected);

            Serializer.Serialize(ref v, ref ctx, writer);
        }

        static byte[] GetExpected(string value)
        {
            var stringBytes = new UTF8Encoding(false).GetBytes(value);
            return PrefixShortLength(stringBytes);
        }

        static byte[] PrefixShortLength(IEnumerable<byte> bytes)
        {
            var length = bytes.Count();
            return BitConverter.GetBytes((short)length).Concat(bytes).ToArray();
        }

        class AssertingWriter : IWriter<object>
        {
            readonly object context;
            readonly byte[] expected;

            public AssertingWriter(object context, params byte[] expected)
            {
                this.context = context;
                this.expected = expected;
            }

            public void Write(ref object ctx, Span<byte> bytes)
            {
                Assert.AreEqual(context, ctx);

                var array = bytes.ToArray();

                CollectionAssert.AreEqual(expected, array);
            }
        }

        class OneFieldClass<T>
        {
            public OneFieldClass(T a)
            {
                A = a;
            }

            [DataMember(Order = F1)]
            public T A;
        }

        struct OneFieldStruct<T>
        {
            public OneFieldStruct(T a)
            {
                A = a;
            }

            [DataMember(Order = F1)]
            public T A;
        }
    }
}
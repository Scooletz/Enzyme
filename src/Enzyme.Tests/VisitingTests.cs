using System;
using System.Collections.Generic;
using System.Text;
using Enzyme.Visiting;
using Enzyme.Writers;
using NUnit.Framework;

namespace Enzyme.Tests
{
    public class VisitingTests
    {
        [Test]
        public void Few_values()
        {
            var guid = new Guid("35ED53A9-A0C2-4C42-A4FA-CDB0A5E8E51B");
            var g = guid.ToByteArray();

            var payload = new byte[] {
                1, Manifests.BoolType, 1,
                2, Manifests.Int32Type, 4,
                3, Manifests.StringType, 4, 0, (byte)'s', (byte)'o', (byte)'m', (byte)'e',
                4, Manifests.Nullable | Manifests.BoolType, 1,
                5, Manifests.GuidType, g[0], g[1], g[2], g[3], g[4], g[5], g[6], g[7], g[8], g[9], g[10], g[11], g[12], g[13], g[14], g[15]
            };

            Run(payload, v =>
            {
                v.On(1, true);
                v.On(2, 2); // zig from 4uint is 2 int
                v.On(3, Encoding.UTF8.GetBytes("some"));
                v.On(4, true);
                v.On(5, guid);
            });
        }

        [Test]
        public void Nested_object()
        {
            var payload = new byte[]
            {
                1, Manifests.Object,
                /*->*/ 3, 0, 4, Manifests.BoolType, 1
            };

            Run(payload, v =>
            {
                v.PushField(1);
                v.On(4, true);
                v.PopField();
            });
        }

        [Test]
        public void Array_ints()
        {
            const int value = 256;
            var payload = new byte[] { 17, Manifests.Int32Type | Manifests.Array, 2, 0, 128, 4 };

            Run(payload, v =>
            {
                v.On(17, value);
            });
        }

        [Test]
        public void Array_longs()
        {
            const long value = long.MaxValue;

            var payload = new byte[] { 17, Manifests.Int64Type | Manifests.Array, 10, 0, 254, 255, 255, 255, 255, 255, 255, 255, 255, 1 };

            Run(payload, v =>
            {
                v.On(17, value);
            });
        }

        [Test]
        public void Nested_with_following_fields()
        {
            var payload = new byte[]
            {
                1, Manifests.Object,
                /*->*/ 3, 0, 4, Manifests.BoolType, 1,
                5, Manifests.Int32Type, 2
            };

            Run(payload, v =>
            {
                v.PushField(1);
                v.On(4, true);
                v.PopField();

                v.On(5, 1);
            });
        }

        static void Run(byte[] payload, Action<AssertingVisitor> prepare)
        {
            var visitor = new AssertingVisitor(0);

            prepare(visitor);

            visitor.Record();

            Visitor.Visit(ref visitor, new Memory<byte>(payload));

            visitor.Assert();
        }

        struct AssertingVisitor : IVisitor
        {
            readonly List<object> expected;
            readonly List<object> recorded;
            bool record;

            static readonly object Null = new object();
            static readonly object ScopeStart = new object();
            static readonly object ScopeEnd = new object();

            public AssertingVisitor(int _)
            {
                expected = new List<object>();
                recorded = new List<object>();
                record = false;
            }

            public void Record()
            {
                record = true;
            }

            public void Assert() => CollectionAssert.AreEqual(expected.ToArray(), recorded.ToArray());

            public void PushField(byte fieldNumber)
            {
                OnValue(ScopeStart);
                OnValue(fieldNumber);
            }

            public void PopField()
            {
                OnValue(ScopeEnd);
            }

            public void On(byte field, object value)
            {
                PushField(field);
                OnValue(value);
                PopField();
            }

            public void On(bool value) => OnValue(value);

            public void On(byte value) => OnValue(value);

            public void On(sbyte value) => OnValue(value);

            public void On(short value) => OnValue(value);

            public void On(ushort value) => OnValue(value);

            public void On(int value) => OnValue(value);

            public void On(uint value) => OnValue(value);

            public void On(long value) => OnValue(value);

            public void On(ulong value) => OnValue(value);

            public void On(DateTime value) => OnValue(value);

            public void On(Guid value) => OnValue(value);

            public void OnNull() => OnValue(Null);

            public void On(Span<byte> utf8) => OnValue(Encoding.UTF8.GetString(utf8));

            void OnValue(object value) => (record ? recorded : expected).Add(value);
        }
    }
}
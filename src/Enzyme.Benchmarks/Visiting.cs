using System;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using Enzyme.Visiting;
using Enzyme.Writers;

namespace Enzyme.Benchmarks
{
    public class Visiting
    {
        readonly GCHandle gcHandle;
        readonly Memory<byte> memory;

        public Visiting()
        {
            var payload = new byte[]
            {
                1, 13,
                /*->*/ 3, 0, 4, 1, 1
            };

            gcHandle = GCHandle.Alloc(payload, GCHandleType.Pinned);
            memory = MemoryMarshal.CreateFromPinnedArray(payload, 0, payload.Length);
        }

        [Benchmark]
        public void Visit()
        {
            var v = new StackVisitor();
            Visitor.Visit(ref v, memory);
        }

        public struct StackVisitor : IVisitor
        {
            long fields;

            public void PushField(byte fieldNumber)
            {
                fields = (fields << 8) | fieldNumber;
            }

            public void PopField()
            {
                fields >>= 8;
            }

            public void On(bool value)
            {
            }

            public void On(byte value)
            {
            }

            public void On(sbyte value)
            {
            }

            public void On(short value)
            {
            }

            public void On(ushort value)
            {
            }

            public void On(int value)
            {
            }

            public void On(uint value)
            {
            }

            public void On(long value)
            {
            }

            public void On(ulong value)
            {
            }

            public void On(DateTime value)
            {
            }

            public void On(Guid value)
            {
            }

            public void On(Span<byte> utf8)
            {
            }

            public void OnNull()
            {
            }
        }
    }
}
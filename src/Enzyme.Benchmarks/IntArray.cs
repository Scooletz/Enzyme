using System.IO;
using System.Runtime.Serialization;
using BenchmarkDotNet.Attributes;
using ProtoBuf;
using ZeroFormatter;

namespace Enzyme.Benchmarks
{
    public class IntArray
    {
        byte[] bytes = new byte[1024];
        readonly ValueVirtual valueVirtual;

        Value value;
        readonly NullWriter nullWriter = new NullWriter();
        object context = new object();

        readonly MemoryStream memoryStream;

        public IntArray()
        {
            valueVirtual = new ValueVirtual
            {
                Values = new[] { 1, 2, 3, 4, 5, 6, 7, 8 }
            };

            value = new Value
            {
                Values = new[] { 1, 2, 3, 4, 5, 6, 7, 8 }
            };

            memoryStream = new MemoryStream();
            memoryStream.SetLength(1024);
        }

        [Benchmark]
        public void ZeroFormatter()
        {
            ZeroFormatterSerializer.Serialize(ref bytes, 0, valueVirtual);
        }

        [Benchmark]
        public void ProtoBufNet()
        {
            memoryStream.Position = 0;
            ProtoBuf.Serializer.Serialize(memoryStream, value);
        }

        [Benchmark]
        public void Enzyme()
        {
            Serializer.Serialize(ref value, ref context, nullWriter);
        }

        [ZeroFormattable]
        public class ValueVirtual
        {
            [Index(0)]
            public virtual int[] Values { get; set; }
        }

        [DataContract]
        public class Value
        {
            [DataMember(Order = 1)] public int[] Values;
        }
    }
}
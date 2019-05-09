using System;
using System.IO;
using System.Runtime.Serialization;
using BenchmarkDotNet.Attributes;
using ZeroFormatter;

namespace Enzyme.Benchmarks
{
    public class GuidArray
    {
        byte[] bytes = new byte[2048];
        readonly ValueVirtual valueVirtual;

        Value value;
        readonly NullWriter nullWriter = new NullWriter();
        object context = new object();

        readonly MemoryStream memoryStream;
        static readonly Guid[] Values = { Guid.Empty, Guid.Empty, Guid.Empty, Guid.Empty, Guid.Empty, Guid.Empty, Guid.Empty, Guid.Empty };

        public GuidArray()
        {
            valueVirtual = new ValueVirtual
            {
                Values = Values
            };

            value = new Value
            {
                Values = Values
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
            public virtual Guid[] Values { get; set; }
        }

        [DataContract]
        public class Value
        {
            [DataMember(Order = 1)] public Guid[] Values;
        }
    }
}
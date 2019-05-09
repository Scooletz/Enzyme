using System;
using System.IO;
using System.Runtime.Serialization;
using BenchmarkDotNet.Attributes;
using ZeroFormatter;

namespace Enzyme.Benchmarks
{
    public class BoolIntStringGuidNullable
    {
        byte[] bytes = new byte[256];
        readonly ValueVirtual valueVirtual;

        Value value;
        readonly NullWriter writer = new NullWriter();
        object context = new object();

        readonly MemoryStream memoryStream;

        public BoolIntStringGuidNullable()
        {
            valueVirtual = new ValueVirtual
            {
                Bool = true,
                Int = 2,
                String = "some",
                NullBool = true,
                Id = new Guid("35ED53A9-A0C2-4C42-A4FA-CDB0A5E8E51B")
            };

            value = new Value
            {
                Bool = true,
                Int = 2,
                String = "some",
                NullBool = true,
                Id = new Guid("35ED53A9-A0C2-4C42-A4FA-CDB0A5E8E51B")
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
            Serializer.Serialize(ref value, ref context, writer);
        }

        [ZeroFormattable]
        public class ValueVirtual
        {
            [Index(0)]
            public virtual bool Bool { get; set; }

            [Index(1)]
            public virtual int Int { get; set; }

            [Index(2)]
            public virtual string String { get; set; }

            [Index(3)]
            public virtual bool NullBool { get; set; }

            [Index(4)]
            public virtual Guid Id { get; set; }
        }

        [DataContract]
        public class Value
        {
            [DataMember(Order = 1)]
            public bool Bool;

            [DataMember(Order = 2)]
            public int Int;

            [DataMember(Order = 3)]
            public string String;

            [DataMember(Order = 4)]
            public bool? NullBool;

            [DataMember(Order = 5)]
            public Guid Id;
        }
    }
}
using System.IO;
using System.Runtime.Serialization;
using BenchmarkDotNet.Attributes;
using ZeroFormatter;

namespace Enzyme.Benchmarks
{
    public class StringArray
    {
        byte[] bytes = new byte[2048];
        readonly ValueVirtual valueVirtual;

        Value value;
        readonly NullWriter nullWriter = new NullWriter();
        object context = new object();

        readonly MemoryStream memoryStream;
        static readonly string[] Values = { "test1", "test2", "test3", "test4", "test5", "test6", "test7", "test8" };

        public StringArray()
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
            public virtual string[] Values { get; set; }
        }

        [DataContract]
        public class Value
        {
            [DataMember(Order = 1)] public string[] Values;
        }
    }
}
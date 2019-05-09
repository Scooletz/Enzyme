using System;

namespace Enzyme.Benchmarks
{
    class NullWriter : IWriter<object>
    {
        public void Write(ref object context, Span<byte> payload) { }
    }
}
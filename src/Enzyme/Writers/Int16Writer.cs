using System;
using System.Buffers.Binary;
using System.Reflection.Emit;
using Enzyme.Visiting;

namespace Enzyme.Writers
{
    class Int16Writer : FixedPrimitiveWriter<short>
    {
        public override byte Manifest => Manifests.Int16Type;
        public override int Size => 2;

        public override void EmitWriteImpl(WriterEmitContext context)
        {
            context.Il.StoreIndirectLittleEndian(typeof(short));
        }

        protected override unsafe void AcceptImpl<TVisitor>(byte* payload, ref TVisitor visitor)
        {
            visitor.On(BinaryPrimitives.ReadInt16LittleEndian(new ReadOnlySpan<byte>(payload, 2)));
        }
    }
}
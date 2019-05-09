using System;
using System.Buffers.Binary;
using System.Reflection.Emit;
using Enzyme.Visiting;

namespace Enzyme.Writers
{
    class UInt16Writer : FixedPrimitiveWriter<ushort>
    {
        public override byte Manifest => Manifests.UInt16Type;
        public override int Size => 2;

        public override void EmitWriteImpl(WriterEmitContext context)
        {
            context.Il.StoreIndirectLittleEndian(typeof(ushort));
        }

        protected override unsafe void AcceptImpl<TVisitor>(byte* payload, ref TVisitor visitor)
        {
            visitor.On(BinaryPrimitives.ReadUInt16LittleEndian(new ReadOnlySpan<byte>(payload, 2)));
        }
    }
}
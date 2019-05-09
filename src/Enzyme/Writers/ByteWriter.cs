using System.Reflection.Emit;
using Enzyme.Visiting;

namespace Enzyme.Writers
{
    class ByteWriter : FixedPrimitiveWriter<byte>
    {
        public override byte Manifest => Manifests.ByteType;
        public override int Size => 1;

        public override void EmitWriteImpl(WriterEmitContext context)
        {
            context.Il.Emit(OpCodes.Stind_I1);
        }

        protected override unsafe void AcceptImpl<TVisitor>(byte* payload, ref TVisitor visitor)
        {
            visitor.On(*payload);
        }
    }
}
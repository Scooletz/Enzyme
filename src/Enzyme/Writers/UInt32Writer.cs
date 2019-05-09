using Enzyme.Visiting;

namespace Enzyme.Writers
{
    class UInt32Writer : BoundedPrimitiveWriter<uint>
    {
        public override byte Manifest => Manifests.UInt32Type;
        public override int Size => 5;

        public override void EmitWriteImpl(WriterEmitContext context)
        {
            context.CallWriteUInt32Variant();
        }

        public override unsafe int Accept<TVisitor>(byte* payload, ref TVisitor visitor)
        {
            var offset = EncodingHelper.TryReadUInt32VariantWithoutMoving(payload, out var value);
            visitor.On(value);
            return offset;
        }
    }
}
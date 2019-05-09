using Enzyme.Visiting;

namespace Enzyme.Writers
{
    class Int32Writer : BoundedPrimitiveWriter<int>
    {
        public override byte Manifest => Manifests.Int32Type;
        public override int Size => 5;

        public override void EmitWriteImpl(WriterEmitContext context)
        {
            context.CallZigInt();
            context.CallWriteUInt32Variant();
        }

        public override unsafe int Accept<TVisitor>(byte* payload, ref TVisitor visitor)
        {
            var offset = EncodingHelper.TryReadUInt32VariantWithoutMoving(payload, out var value);
            visitor.On(EncodingHelper.ZagToInt(value));
            return offset;
        }
    }
}
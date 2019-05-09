using Enzyme.Visiting;

namespace Enzyme.Writers
{
    class Int64Writer : BoundedPrimitiveWriter<long>
    {
        public override byte Manifest => Manifests.Int64Type;
        public override int Size => 9;

        public override void EmitWriteImpl(WriterEmitContext context)
        {
            context.CallZigLong();
            context.CallWriteUInt64Variant();
        }

        public override unsafe int Accept<TVisitor>(byte* payload, ref TVisitor visitor)
        {
            var offset = EncodingHelper.TryReadUInt64VariantWithoutMoving(payload, out var value);
            visitor.On(EncodingHelper.ZagToLong(value));
            return offset;
        }
    }
}
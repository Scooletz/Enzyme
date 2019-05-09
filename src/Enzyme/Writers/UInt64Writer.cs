using Enzyme.Visiting;

namespace Enzyme.Writers
{
    class UInt64Writer : BoundedPrimitiveWriter<ulong>
    {
        public override byte Manifest => Manifests.UInt64Type;
        public override int Size => 9;

        public override void EmitWriteImpl(WriterEmitContext context)
        {
            context.CallWriteUInt64Variant();
        }

        public override unsafe int Accept<TVisitor>(byte* payload, ref TVisitor visitor)
        {
            var offset = EncodingHelper.TryReadUInt64VariantWithoutMoving(payload, out var value);
            visitor.On(value);
            return offset;
        }
    }
}
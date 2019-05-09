using System;
using System.Reflection.Emit;
using Enzyme.Visiting;

namespace Enzyme.Writers
{
    class GuidWriter : FixedPrimitiveWriter<Guid>
    {
        public override byte Manifest => Manifests.GuidType;
        public override int Size => 16;

        public override void EmitWriteImpl(WriterEmitContext context)
        {
            context.Il.Emit(OpCodes.Stobj, typeof(Guid));
        }

        protected override unsafe void AcceptImpl<TVisitor>(byte* payload, ref TVisitor visitor)
        {
#if NETSTANDARD
            var data = new byte[16];
            var readOnlySpan = new ReadOnlySpan<byte>(payload, 16);
            readOnlySpan.CopyTo(data);
            visitor.On(new Guid(data));
#else
            visitor.On(new Guid(new ReadOnlySpan<byte>(payload, 16)));
#endif
        }
    }
}
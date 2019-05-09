using System;
using System.Buffers.Binary;
using System.Reflection;
using System.Reflection.Emit;
using Enzyme.Visiting;

namespace Enzyme.Writers
{
    class DateTimeWriter : FixedPrimitiveWriter<DateTime>
    {
        static readonly MethodInfo GetTicks = typeof(DateTime).GetProperty(nameof(DateTime.Ticks)).GetGetMethod();

        public override byte Manifest => Manifests.DateTime;
        public override int Size => 8;

        public override void EmitWriteImpl(WriterEmitContext context)
        {
            context.Il.EmitCall(OpCodes.Call, GetTicks, null);
            context.Il.StoreIndirectLittleEndian(typeof(long));
        }

        public override bool RequiresAddress => true;

        protected override unsafe void AcceptImpl<TVisitor>(byte* payload, ref TVisitor visitor)
        {
            visitor.On(new DateTime(BinaryPrimitives.ReadInt64LittleEndian(new ReadOnlySpan<byte>(payload, 8))));
        }
    }
}
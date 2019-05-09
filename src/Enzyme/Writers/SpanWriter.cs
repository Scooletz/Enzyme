using System;
using System.Reflection.Emit;

namespace Enzyme.Writers
{
    class SpanWriter<T> : EnumerableLikeWriter
    {
        public SpanWriter(IWriter itemWriter)
            : base(itemWriter)
        { }

        public override Type Type => typeof(Span<T>);
        public override bool RequiresAddress => true;

        protected override void EmitCount(WriterEmitContext context)
        {
            context.LoadValue();
            context.Il.EmitCall(OpCodes.Call, typeof(Span<T>).GetProperty(nameof(Span<T>.Length)).GetGetMethod(), null);
        }
    }

    class ByteSpanWriter : IVarSizeWriter
    {
        public Type Type => typeof(Span<byte>);
        public byte Manifest => Manifests.Array | Manifests.ByteType;
        public bool RequiresAddress => true;

        public void EmitWrite(WriterEmitContext context)
        {
            var il = context.Il;
            using (context.GetLocal<int>(out var length))
            {
                context.LoadValue();
                il.EmitCall(OpCodes.Call, typeof(Span<byte>).GetProperty(nameof(Span<byte>.Length)).GetGetMethod(), null);
                il.Store(length);

                context.LoadValue(); // stack: span

                context.LoadBuffer(); // stack: span, buffer
                il.Load(length); // stack: span, buffer, length

                il.Emit(OpCodes.Newobj, typeof(Span<byte>).GetConstructor(new[] { typeof(void).MakePointerType(), typeof(int) })); //stack: span-source, span-dest

                il.EmitCall(OpCodes.Call, typeof(Span<byte>).GetMethod(nameof(Span<byte>.CopyTo)), null);
                il.Load(length);
                context.AddToCurrentOffset();
            }
        }

        public void EmitSizeEstimator(WriterEmitContext context)
        {
            context.LoadValue();
            context.Il.EmitCall(OpCodes.Call, typeof(Span<byte>).GetProperty(nameof(Span<byte>.Length)).GetGetMethod(), null);
        }
    }
}
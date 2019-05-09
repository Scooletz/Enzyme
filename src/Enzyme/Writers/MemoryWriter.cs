using System;
using System.Reflection.Emit;

namespace Enzyme.Writers
{
    class MemoryWriter<T> : IVarSizeWriter
    {
        readonly IVarSizeWriter spanWriter;

        public MemoryWriter(IWriter itemWriter)
        {
            if (itemWriter.Type == typeof(byte))
            {
                spanWriter = new ByteSpanWriter();
            }
            else
            {
                spanWriter = new SpanWriter<T>(itemWriter);
            }
        }

        public Type Type => typeof(Memory<T>);
        public byte Manifest => spanWriter.Manifest;
        public bool RequiresAddress => true;
        public void EmitWrite(WriterEmitContext context)
        {
            using (context.GetLocal(typeof(Span<T>), out var span))
            {
                context.LoadValue();
                context.Il.EmitCall(OpCodes.Call, Type.GetProperty(nameof(Memory<T>.Span)).GetGetMethod(), null);
                context.Il.Store(span);

                using (context.OverrideScope(spanWriter, span))
                {
                    spanWriter.EmitWrite(context);
                }
            }
        }

        public void EmitSizeEstimator(WriterEmitContext context)
        {
            context.LoadValue();
            context.Il.EmitCall(OpCodes.Call, typeof(Memory<T>).GetProperty(nameof(Memory<T>.Length)).GetGetMethod(), null);
        }
    }
}
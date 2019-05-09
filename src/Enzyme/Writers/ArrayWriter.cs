using System;
using System.Reflection.Emit;

namespace Enzyme.Writers
{
    abstract class ArrayWriter<T> : IVarSizeWriter
    {
        readonly IWriter itemWriter;

        protected ArrayWriter(IWriter itemWriter)
        {
            this.itemWriter = itemWriter;
            Manifest = (byte)(Manifests.Array | itemWriter.Manifest);
        }

        public Type Type => typeof(T[]);
        public byte Manifest { get; }
        public bool RequiresAddress => false;

        public void EmitWrite(WriterEmitContext context)
        {
            LoopOver(context, WriteValue);
        }

        protected void LoopOver(WriterEmitContext context, Action<WriterEmitContext, Action> useValue)
        {
            var il = context.Il;

            using (context.GetLocal<int>(out var i))
            {
                var check = il.DefineLabel();
                var start = il.DefineLabel();

                il.LoadIntValue(0);
                il.Store(i);

                il.Emit(OpCodes.Br, check);

                il.MarkLabel(start);

                void LoadElementValue()
                {
                    il.Load(i);

                    if (itemWriter.RequiresAddress)
                    {
                        il.Emit(OpCodes.Ldelema, typeof(T));
                    }
                    else
                    {
                        il.LoadElem(typeof(T));
                    }
                }

                useValue(context, LoadElementValue);

                // i++
                il.LoadIntValue(1);
                il.Load(i);
                il.Emit(OpCodes.Add);
                il.Store(i);

                // check boundaries
                il.MarkLabel(check);

                il.Load(i);
                context.LoadValue();
                il.Emit(OpCodes.Ldlen);
                il.Emit(OpCodes.Conv_I4);
                il.Emit(OpCodes.Blt, start);
            }
        }

        public abstract void EmitSizeEstimator(WriterEmitContext context);

        void WriteValue(WriterEmitContext context, Action loadValue)
        {
            using (context.Scope(loadValue))
            {
                itemWriter.EmitWrite(context);
            }

            if (itemWriter is IUpdateOffset == false)
            {
                context.AddToCurrentOffset();
            }
        }
    }

    class BoundedSizeArrayWriter<T> : ArrayWriter<T>
    {
        readonly IBoundedSizeWriter itemWriter;

        public BoundedSizeArrayWriter(IBoundedSizeWriter itemWriter)
            : base(itemWriter)
        {
            this.itemWriter = itemWriter;
        }

        public override void EmitSizeEstimator(WriterEmitContext context)
        {
            context.LoadValue();

            var il = context.Il;
            il.Emit(OpCodes.Ldlen);
            il.Emit(OpCodes.Conv_I4);

            il.LoadIntValue(itemWriter.Size);
            il.Emit(OpCodes.Mul);
            il.Emit(OpCodes.Conv_Ovf_I2);

            // add 2 for the count
            il.LoadIntValue(2);
            il.Emit(OpCodes.Add);
        }
    }

    class VarSizeArrayWriter<T> : ArrayWriter<T>
    {
        readonly IVarSizeWriter itemWriter;

        public VarSizeArrayWriter(IVarSizeWriter itemWriter)
            : base(itemWriter)
        {
            this.itemWriter = itemWriter;
        }

        public override void EmitSizeEstimator(WriterEmitContext context)
        {
            using (context.GetLocal<int>(out var sum))
            {
                LoopOver(context, (ctx, loadValue) =>
                {
                    using (context.Scope(loadValue))
                    {
                        itemWriter.EmitSizeEstimator(ctx);
                    }

                    // stack: size
                    ctx.Il.Load(sum);
                    ctx.Il.Emit(OpCodes.Add);
                    ctx.Il.Store(sum); // add and store in the sum
                });

                var il = context.Il;

                il.Load(sum);
                // add 2 for the count
                il.LoadIntValue(2);
                il.Emit(OpCodes.Add);
            }
        }
    }
}
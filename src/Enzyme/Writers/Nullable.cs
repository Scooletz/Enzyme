using System;
using System.Reflection.Emit;

namespace Enzyme.Writers
{
    abstract class NullableWriter<TPrimitive> : IBoundedSizeWriter, IWriteManifest
        where TPrimitive : struct
    {
        readonly IWriter wrapped;

        protected NullableWriter(IWriter wrapped)
        {
            this.wrapped = wrapped;
        }

        public Type Type => typeof(TPrimitive?);

        public byte Manifest => (byte)(wrapped.Manifest | Manifests.Nullable);
        public bool RequiresAddress => true;
        public abstract int Size { get; }

        public void EmitWrite(WriterEmitContext context)
        {
            var nullable = typeof(TPrimitive?);

            var il = context.Il;

            var hasValue = il.DefineLabel();
            var end = il.DefineLabel();

            context.LoadValue();
            il.EmitCall(OpCodes.Call, nullable.GetProperty(nameof(Nullable<int>.HasValue)).GetGetMethod(), null);
            il.Emit(OpCodes.Brtrue_S, hasValue);

            // no value, just store 0 and return length = 1

            if (context.HasManifestToWrite == false)
            {
                context.LoadBuffer();
                il.LoadIntValue(0);
                il.Emit(OpCodes.Stind_I1);
                il.LoadIntValue(1);
                il.Emit(OpCodes.Br, end); // jump to the end of writing
            }
            else
            {
                il.LoadIntValue(0);
                il.Emit(OpCodes.Br, end); // jump to the end of writing
            }

            // has value, store 1, then store value
            il.MarkLabel(hasValue);

            if (context.HasManifestToWrite == false)
            {
                context.LoadBuffer();
                il.LoadIntValue(1);
                il.Emit(OpCodes.Stind_I1);

                il.LoadIntValue(1);
                context.AddToCurrentOffset();
            }
            else
            {
                context.WriteManifest();
            }

            // load shifted buffer and value
            context.LoadBuffer();
            context.LoadValue();
            il.EmitCall(OpCodes.Call, nullable.GetProperty(nameof(Nullable<int>.Value)).GetGetMethod(), null);

            // emit regular
            Emit(context);
            il.MarkLabel(end);
        }

        protected abstract void Emit(WriterEmitContext context);
    }

    class NullableFixedWriter<TPrimitive> : NullableWriter<TPrimitive>
        where TPrimitive : struct
    {
        readonly FixedPrimitiveWriter<TPrimitive> wrapped;

        public override int Size => wrapped.Size + 1;

        public NullableFixedWriter(FixedPrimitiveWriter<TPrimitive> wrapped)
            : base(wrapped)
        {
            this.wrapped = wrapped;
        }

        protected override void Emit(WriterEmitContext context)
        {
            wrapped.EmitWriteImpl(context);
            context.Il.LoadIntValue(wrapped.Size);
        }
    }

    class NullableBoundedWriter<TPrimitive> : NullableWriter<TPrimitive>
        where TPrimitive : struct
    {
        readonly BoundedPrimitiveWriter<TPrimitive> wrapped;

        public override int Size => wrapped.Size + 1;

        public NullableBoundedWriter(BoundedPrimitiveWriter<TPrimitive> wrapped)
            : base(wrapped)
        {
            this.wrapped = wrapped;
        }

        protected override void Emit(WriterEmitContext context)
        {
            wrapped.EmitWriteImpl(context);
        }
    }
}
using System;
using System.Buffers.Binary;
using System.Reflection.Emit;
using Enzyme.Visiting;

namespace Enzyme.Writers
{
    abstract class StoreLengthDecorator : IWriteManifest, IUpdateOffset, IAcceptVisitor
    {
        const int NullLength = -1;
        public readonly IWriter Writer;

        protected StoreLengthDecorator(IWriter writer)
        {
            Writer = writer;
        }

        public Type Type => Writer.Type;
        public bool RequiresAddress => Writer.RequiresAddress;
        public byte Manifest => Writer.Manifest;

        public void EmitWrite(WriterEmitContext context)
        {
            if (Type.IsValueType)
            {
                EmitNonNullWriter(context);
            }
            else
            {
                EmitClassWriter(context);
            }
        }

        void EmitClassWriter(WriterEmitContext ctx)
        {
            var il = ctx.Il;
            var nullLbl = il.DefineLabel();
            var end = il.DefineLabel();

            ctx.LoadValue();
            il.Emit(OpCodes.Brfalse, nullLbl);

            EmitNonNullWriter(ctx);
            il.Emit(OpCodes.Br, end);

            // writing null
            il.MarkLabel(nullLbl);
            if (ctx.HasManifestToWrite == false)
            {
                ctx.LoadBuffer();
                il.LoadIntValue(NullLength);
                il.StoreIndirectLittleEndian(typeof(short));
                il.LoadIntValue(2); // used only 2 bytes for writing null
                ctx.AddToCurrentOffset();
            }
            else
            {
                // no value, nothing to write
            }

            il.MarkLabel(end);
        }

        void EmitNonNullWriter(WriterEmitContext ctx)
        {
            var il = ctx.Il;

            if (ctx.HasManifestToWrite)
            {
                ctx.WriteManifest();
            }

            using (ctx.GetLocal<int>(out var offset))
            {
                ctx.LoadCurrentOffset();
                il.Store(offset);

                // add 2 for the length of variable
                il.LoadIntValue(2);
                ctx.AddToCurrentOffset();

                Writer.EmitWrite(ctx);

                // stack empty, value written directly to the buffer

                ctx.LoadBuffer(offset); // stack: byte*

                ctx.LoadCurrentOffset();
                il.Load(offset);
                il.Emit(OpCodes.Sub); // stack: byte*, value

                il.LoadIntValue(2);
                il.Emit(OpCodes.Sub); // -2 for length

                il.StoreIndirectLittleEndian(typeof(short)); // save length
            }
        }

        public unsafe int Accept<TVisitor>(byte* payload, ref TVisitor visitor)
            where TVisitor : struct, IVisitor
        {
            if (TryReadLength(ref payload, out var length) == false)
            {
                visitor.OnNull();
                return 2;
            }

            if (Writer is IAcceptLimitedLengthVisitor limited)
            {
                limited.Accept(payload, length, ref visitor);
                return length + 2;
            }

            return 2;
        }

        public static unsafe bool TryReadLength(ref byte* payload, out short length)
        {
            length = BinaryPrimitives.ReadInt16LittleEndian(new ReadOnlySpan<byte>(payload, 2));
            if (length == NullLength)
            {
                length = 0;

                payload += 2;
                return false;
            }

            payload += 2;
            return true;
        }
    }

    class ObjectBoundedSizedStoreLengthDecorator : StoreLengthDecorator, IBoundedSizeWriter
    {
        readonly IBoundedSizeWriter writer;

        public ObjectBoundedSizedStoreLengthDecorator(IBoundedSizeWriter writer)
            : base(writer)
        {
            this.writer = writer;
        }

        public int Size => writer.Size + 2;
    }

    class VarSizedStoreLengthDecorator : StoreLengthDecorator, IVarSizeWriter
    {
        readonly IVarSizeWriter writer;

        public VarSizedStoreLengthDecorator(IVarSizeWriter writer) : base(writer)
        {
            this.writer = writer;
        }

        public void EmitSizeEstimator(WriterEmitContext context)
        {
            if (Type.IsValueType)
            {
                writer.EmitSizeEstimator(context);
            }
            else
            {
                var il = context.Il;
                var nullLbl = il.DefineLabel();
                var end = il.DefineLabel();

                context.LoadValue();
                il.Emit(OpCodes.Brfalse, nullLbl);

                writer.EmitSizeEstimator(context);
                il.LoadIntValue(2);
                il.Emit(OpCodes.Add);

                il.Emit(OpCodes.Br_S, end);

                il.MarkLabel(nullLbl);
                il.LoadIntValue(2);

                il.MarkLabel(end);
            }
        }
    }
}
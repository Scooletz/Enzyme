using System;
using Enzyme.Visiting;

namespace Enzyme.Writers
{
    /// <summary>
    /// The base writer interface.
    /// </summary>
    interface IWriter
    {
        Type Type { get; }

        /// <summary>
        /// Gets the manifest value.
        /// </summary>
        byte Manifest { get; }

        bool RequiresAddress { get; }

        /// <summary>
        /// Emits IL responsible for writing the value. Methods that should be used are <see cref="WriterEmitContext.LoadBuffer"/> and <see cref="WriterEmitContext.LoadValue"/>.
        /// </summary>
        /// <param name="context"></param>
        void EmitWrite(WriterEmitContext context);
    }

    /// <summary>
    /// A writer for a type that has a bounded size, but because of the space efficiency might be written on lower number of bytes.
    /// </summary>
    interface IBoundedSizeWriter : IWriter
    {
        /// <summary>
        /// Gets the maximum size that the entry will take.
        /// </summary>
        int Size { get; }
    }

    /// <summary>
    /// A markup interface for writers writing manifest on their own.
    /// </summary>
    interface IWriteManifest : IWriter {}

    /// <summary>
    /// A markup interface for writers that update offset on their own.
    /// </summary>
    interface IUpdateOffset : IWriter {}

    /// <summary>
    /// A writer for a type that has a variable size of encoding. It provides both, the estimation and the write methods.
    /// </summary>
    interface IVarSizeWriter : IWriter
    {
        // Emits the upper boundary of the size that is needed for writing the value.
        void EmitSizeEstimator(WriterEmitContext context);
    }

    abstract class FixedPrimitiveWriter<TPrimitive> : IBoundedSizeWriter, IAcceptVisitor
    {
        public Type Type => typeof(TPrimitive);
        public abstract byte Manifest { get; }
        public abstract int Size { get; }

        public void EmitWrite(WriterEmitContext context)
        {
            context.LoadBuffer();
            context.LoadValue();

            EmitWriteImpl(context);

            context.Il.LoadIntValue(Size);
        }

        public abstract void EmitWriteImpl(WriterEmitContext context);

        public virtual bool RequiresAddress => false;

        public unsafe int Accept<TVisitor>(byte* payload, ref TVisitor visitor)
            where TVisitor : struct, IVisitor
        {
            AcceptImpl(payload, ref visitor);
            return Size;
        }

        protected abstract unsafe void AcceptImpl<TVisitor>(byte* payload, ref TVisitor visitor)
            where TVisitor : struct, IVisitor;
    }

    abstract class BoundedPrimitiveWriter<TPrimitive> : IBoundedSizeWriter, IAcceptVisitor
    {
        public Type Type => typeof(TPrimitive);
        public bool RequiresAddress => false;

        public abstract byte Manifest { get; }
        public abstract int Size { get; }

        public void EmitWrite(WriterEmitContext context)
        {
            context.LoadBuffer();
            context.LoadValue();

            EmitWriteImpl(context);
        }

        public abstract void EmitWriteImpl(WriterEmitContext context);

        public abstract unsafe int Accept<TVisitor>(byte* payload, ref TVisitor visitor)
            where TVisitor : struct, IVisitor;
    }
}
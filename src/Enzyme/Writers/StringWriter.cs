using System;
using System.Reflection.Emit;
using Enzyme.Visiting;

namespace Enzyme.Writers
{
    class StringWriter : IVarSizeWriter, IAcceptLimitedLengthVisitor
    {
        public Type Type => typeof(string);
        public byte Manifest => Manifests.StringType;
        public bool RequiresAddress => false;

        public void EmitWrite(WriterEmitContext context)
        {
            context.LoadBuffer();
            context.LoadValue();
            context.CallWriteStringUnsafe();
            context.AddToCurrentOffset();
        }

        public void EmitSizeEstimator(WriterEmitContext context)
        {
            context.LoadValue();
            context.Il.EmitCall(OpCodes.Call, typeof(StringWriter).GetMethod(nameof(CalculateSize)), null);
        }

        public static short CalculateSize(string value) => (short)EncodingHelper.Utf8.GetMaxByteCount(value.Length);

        public unsafe void Accept<TVisitor>(byte* payload, int length, ref TVisitor visitor)
            where TVisitor : struct, IVisitor
        {
            visitor.On(new Span<byte>(payload, length));
        }
    }
}
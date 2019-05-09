using System.Reflection.Emit;
using Enzyme.Visiting;

namespace Enzyme.Writers
{
    class BoolWriter : FixedPrimitiveWriter<bool>
    {
        public override byte Manifest => Manifests.BoolType;
        public override int Size => 1;

        public override void EmitWriteImpl(WriterEmitContext context)
        {
            //IL_0001: brtrue.s IL_0006
            //IL_0003: ldc.i4.0
            //IL_0004: br.s IL_0007
            //IL_0006: ldc.i4.1
            //IL_0007: conv.u1

            var il = context.Il;

            var load1 = il.DefineLabel();

            var conversionU1 = il.DefineLabel();

            il.Emit(OpCodes.Brtrue_S, load1);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Br_S, conversionU1);

            il.MarkLabel(load1);
            il.Emit(OpCodes.Ldc_I4_1);

            il.MarkLabel(conversionU1);
            il.Emit(OpCodes.Conv_U1);

            // store
            il.Emit(OpCodes.Stind_I1);
        }

        protected override unsafe void AcceptImpl<TVisitor>(byte* payload, ref TVisitor visitor)
        {
            visitor.On(*payload > 0);
        }
    }
}
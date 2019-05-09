using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Enzyme.Writers;

namespace Enzyme
{
    class Builder
    {
        public static Write<TContext, TValue> Build<TContext, TValue>()
        {
            return BuildWriter<TContext, TValue, Write<TContext, TValue>>();
        }

        static TDelegate BuildWriter<TContext, TValue, TDelegate>()
            where TDelegate : class
        {
            var valueParameter = typeof(TValue).MakeByRefType();

            var name = "Writer_" + typeof(TValue).FullName.Replace(".", "_");

            var dm = new DynamicMethod(name, typeof(void), new[]
            {
                valueParameter,
                typeof(TContext).MakeByRefType(),
                typeof(IWriter<TContext>)
            }, typeof(IWriter<>).Assembly.Modules.Single(), true);

            var il = dm.GetILGenerator();
            Emit<TContext, TValue>(il);

            return dm.CreateDelegate(typeof(TDelegate)) as TDelegate;
        }

        static void Emit<TContext, TValue>(ILGenerator il)
        {
            var buffer = il.DeclareLocal(typeof(byte).MakePointerType());
            var current = il.DeclareLocal(typeof(int));

            var ctx = new WriterEmitContext(il, buffer, current);
            var w = AllWriters.GetWriter(typeof(TValue));

            Action load;

            if (typeof(TValue).IsValueType)
            {
                load = () => il.Emit(OpCodes.Ldarg_0);
            }
            else
            {
                var local = il.DeclareLocal(typeof(TValue));

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldind_Ref);
                il.Store(local);

                load = () => il.Load(local);
            }

            using (ctx.Scope(load))
            {
                if (w is IBoundedSizeWriter bounded)
                {
                    il.LoadIntValue(bounded.Size);
                }
                else if (w is IVarSizeWriter varSize)
                {
                    varSize.EmitSizeEstimator(ctx);
                }
                else throw new NotImplementedException();

                // header size on the stack
            }

            using (ctx.Scope(load))
            {
                // the length is on tack
                il.Emit(OpCodes.Conv_U);
                il.Emit(OpCodes.Localloc); // stack: ctx, byte*
                il.Store(buffer);

                // undecorate if needed
                if (w is StoreLengthDecorator lengthDecorator)
                {
                    w = lengthDecorator.Writer;
                }

                w.EmitWrite(ctx);

                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldarg_2);
                il.Load(buffer);
                il.Load(current);

                var writeMethod = typeof(Builder).GetMethod(nameof(WriteToWriter), BindingFlags.NonPublic | BindingFlags.Static);
                il.EmitCall(OpCodes.Call, writeMethod.MakeGenericMethod(typeof(TContext)), null);
                il.Emit(OpCodes.Ret);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static unsafe void WriteToWriter<TContext>(ref TContext ctx, IWriter<TContext> writer, byte* payload, int offset)
        {
            writer.Write(ref ctx, new Span<byte>(payload, offset));
        }
    }
}
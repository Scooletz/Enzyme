using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Enzyme.Writers
{
    abstract class EnumerableLikeWriter : IVarSizeWriter
    {
        readonly IWriter itemWriter;
        public byte Manifest { get; }
        public abstract Type Type { get; }
        public abstract bool RequiresAddress { get; }

        protected EnumerableLikeWriter(IWriter itemWriter)
        {
            this.itemWriter = itemWriter;
            Manifest = (byte)(Manifests.Array | itemWriter.Manifest);
        }

        public void EmitWrite(WriterEmitContext context)
        {
            Enumerate(context, () => WriteValue(context));
        }

        protected void Enumerate(WriterEmitContext context, Action consumeValue)
        {
            var il = context.Il;

            var getEnumerator = Type.GetMethod(nameof(IEnumerable<int>.GetEnumerator), BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Instance);

            var enumeratorType = getEnumerator.ReturnType;
            var enumeratorCurrentType = enumeratorType.GetProperty(nameof(IEnumerator<int>.Current)).PropertyType;

            var isEnumeratorByRef = enumeratorCurrentType.IsByRef;

            var current = enumeratorType.GetProperty(nameof(IEnumerator<int>.Current)).GetMethod;
            var moveNext = enumeratorType.GetMethod(nameof(IEnumerator<int>.MoveNext), BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Instance);

            if (moveNext == null && enumeratorType.IsGenericType && enumeratorType.GetGenericTypeDefinition() == typeof(IEnumerator<>))
            {
                moveNext = typeof(IEnumerator).GetMethod(nameof(IEnumerator.MoveNext));
            }

            using (context.GetLocal(enumeratorType, out var enumerator))
            {
                context.LoadValue();
                il.EmitCall(enumeratorType.IsValueType ? OpCodes.Call : OpCodes.Callvirt, getEnumerator, null);
                il.Store(enumerator);

                // try
                var enumeratorIsDisposable = typeof(IDisposable).IsAssignableFrom(enumeratorType);
                if (enumeratorIsDisposable)
                {
                    il.BeginExceptionBlock();
                }

                // iterate over values

                var processLbl = il.DefineLabel();
                var loadLbl = il.DefineLabel();
                var outOfFinally = il.DefineLabel();

                il.Emit(OpCodes.Br, loadLbl);

                il.MarkLabel(processLbl);

                // true, false
                if (itemWriter.RequiresAddress & !isEnumeratorByRef)
                {
                    using (context.GetLocal(itemWriter.Type, out var value))
                    {
                        il.CallOn(enumerator, current);
                        il.Store(value);

                        using (context.OverrideScope(itemWriter, value))
                        {
                            consumeValue();
                        }
                    }
                }

                // true, true
                // false, false
                if (itemWriter.RequiresAddress ^ isEnumeratorByRef == false)
                {
                    void LoadValue()
                    {
                        il.CallOn(enumerator, current);
                    }

                    using (context.Scope(LoadValue, false))
                    {
                        consumeValue();
                    }
                }

                // false, true
                if (!itemWriter.RequiresAddress & isEnumeratorByRef)
                {
                    void LoadValue()
                    {
                        il.CallOn(enumerator, current);
                        il.LoadIndirect(itemWriter.Type);
                    }

                    using (context.Scope(LoadValue, false))
                    {
                        consumeValue();
                    }
                }

                il.MarkLabel(loadLbl);

                il.CallOn(enumerator, moveNext);

                il.Emit(OpCodes.Brtrue, processLbl);

                if (enumeratorIsDisposable)
                {
                    il.Emit(OpCodes.Leave, outOfFinally);

                    // finally
                    il.BeginFinallyBlock();
                    var nullLbl = il.DefineLabel();

                    il.Load(enumerator);
                    il.Emit(OpCodes.Brfalse, nullLbl);

                    il.Load(enumerator);
                    il.EmitCall(OpCodes.Callvirt, typeof(IDisposable).GetMethod(nameof(IDisposable.Dispose)), null);
                    il.MarkLabel(nullLbl);

                    il.EndExceptionBlock();

                    il.MarkLabel(outOfFinally);
                }
            }
        }

        protected abstract void EmitCount(WriterEmitContext context);

        public void EmitSizeEstimator(WriterEmitContext ctx)
        {
            var il = ctx.Il;

            if (itemWriter is IBoundedSizeWriter bounded)
            {
                EmitCount(ctx);
                il.LoadIntValue(bounded.Size);
                il.Emit(OpCodes.Mul);
                il.Emit(OpCodes.Conv_Ovf_I2);

                // add 2 for the count
                il.LoadIntValue(2);
                il.Emit(OpCodes.Add);
            }
            else if (itemWriter is IVarSizeWriter varSize)
            {
                using (ctx.GetLocal<int>(out var sum))
                {
                    // zero value
                    il.LoadIntValue(0);
                    il.Store(sum);

                    Enumerate(ctx, () =>
                    {
                        varSize.EmitSizeEstimator(ctx);

                        // stack: size
                        ctx.Il.Load(sum);
                        ctx.Il.Emit(OpCodes.Add);
                        ctx.Il.Store(sum); // add and store in the sum
                    });

                    il.Load(sum);
                    // add 2 for the count
                    il.LoadIntValue(2);
                    il.Emit(OpCodes.Add);
                }
            }
            else
            {
                throw new NotImplementedException($"The type of writer {itemWriter.GetType()} is not currently handled");
            }
        }

        void WriteValue(WriterEmitContext context)
        {
            itemWriter.EmitWrite(context);

            if (itemWriter is IUpdateOffset == false)
            {
                context.AddToCurrentOffset();
            }
        }
    }
}
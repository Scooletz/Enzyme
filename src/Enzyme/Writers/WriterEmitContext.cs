using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Enzyme.Writers
{
    class WriterEmitContext
    {
        public readonly ILGenerator Il;
        readonly LocalBuilder buffer;
        readonly LocalBuilder offset;

        readonly Dictionary<Type, Stack<LocalBuilder>> locals = new Dictionary<Type, Stack<LocalBuilder>>();

        static readonly Dictionary<string, MethodInfo> EncodingHelperMethods =
            typeof(EncodingHelper).GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .ToDictionary(m => m.Name);

        public WriterEmitContext(ILGenerator il, LocalBuilder buffer, LocalBuilder offset)
        {
            Il = il;
            this.buffer = buffer;
            this.offset = offset;
        }

        public void LoadBuffer()
        {
            Il.Load(buffer);
            Il.Load(offset);
            Il.Emit(OpCodes.Add);
        }

        public void LoadBuffer(LocalBuilder at)
        {
            Il.Load(buffer);
            Il.Load(at);
            Il.Emit(OpCodes.Add);
        }

        public void LoadCurrentOffset()
        {
            Il.Load(offset);
        }

        public void AddToCurrentOffset()
        {
            Il.Load(offset);
            Il.Emit(OpCodes.Add);
            Il.Store(offset);
        }

        public void CallWriteStringUnsafe() => Call(nameof(EncodingHelper.WriteStringUnsafe));
        public void CallZigLong() => Call(nameof(EncodingHelper.ZigLong));
        public void CallZigInt() => Call(nameof(EncodingHelper.ZigInt));
        public void CallWriteUInt32Variant() => Call(nameof(EncodingHelper.WriteUInt32Variant));
        public void CallWriteUInt64Variant() => Call(nameof(EncodingHelper.WriteUInt64Variant));

        void Call(string method)
        {
            Il.EmitCall(OpCodes.Call, EncodingHelperMethods[method], null);
        }

        public IDisposable GetLocal<T>(out LocalBuilder local) => GetLocal(typeof(T), out local);

        public IDisposable GetLocal(Type localType, out LocalBuilder local)
        {
            // this breaks type safety, but allows reusing locals. It's like Unsafe.As but emitted :)
            if ((localType.IsClass || localType.IsInterface) && localType.IsPointer == false)
            {
                localType = typeof(object);
            }

            var stack = locals.GetOrCreate(localType);
            if (stack.TryPop(out local) == false)
            {
                local = Il.DeclareLocal(localType);
            }

            var item = local;
            return new Disposable(() => stack.Push(item));
        }

        public void LoadValue() => current.LoadValue();

        public void WriteManifest() => current.WriteManifest();

        public bool HasManifestToWrite => current.HasManifestToWrite;

        public IDisposable OverrideScope(IWriter valueWriter, LocalBuilder @override)
        {
            void LoadValue()
            {
                if (valueWriter.RequiresAddress)
                {
                    Il.LoadAddress(@override);
                }
                else
                {
                    Il.Load(@override);
                }
            }

            return Scope(LoadValue, false);
        }

        /// <summary>
        /// Creates the current value loading scope, proceeding from the parent till the newly created scope.
        /// Scope walking can be disabled by setting <paramref name="chainUp"/> to false.
        /// </summary>
        /// <param name="loadValue">Loads the current value.</param>
        /// <param name="chainUp">If true, the value will be loaded from the parent object. If not, only currently passed <paramref name="loadValue"/> will be called.</param>
        /// <returns></returns>
        public IDisposable Scope(Action loadValue, bool chainUp = true, ushort? manifest = null)
        {
            return current = new EmitScope(this, loadValue, current, chainUp, manifest);
        }

        EmitScope current;


        class EmitScope : IDisposable
        {
            readonly WriterEmitContext context;
            readonly Action loadValue;
            readonly EmitScope parent;
            readonly bool chainUp;
            readonly ushort? manifest;

            public EmitScope(WriterEmitContext context, Action loadValue, EmitScope parent, bool chainUp, ushort? manifest)
            {
                this.context = context;
                this.loadValue = loadValue;
                this.parent = parent;
                this.chainUp = chainUp;
                this.manifest = manifest;
            }

            public void LoadValue()
            {
                if (chainUp)
                {
                    parent?.LoadValue();
                }

                loadValue();
            }

            public void WriteManifest()
            {
                if (HasManifestToWrite)
                {
                    context.LoadBuffer();
                    context.Il.LoadIntValue(manifest.Value);
                    context.Il.StoreIndirectLittleEndian(typeof(short));

                    context.Il.LoadIntValue(2);
                    context.AddToCurrentOffset();

                }
                else
                {
                    throw new Exception("No manifest to write");
                }
            }

            public bool HasManifestToWrite => manifest != null;

            public void Dispose()
            {
                if (context.current != this)
                {
                    throw new ArgumentException("Scopes released out of order!");
                }

                context.current = parent;
            }
        }
    }
}
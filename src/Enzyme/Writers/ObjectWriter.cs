using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;

namespace Enzyme.Writers
{
    static class ObjectWriterFactory
    {
        public static IWriter Build(Type type)
        {
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public)
                .Where(field => field.GetCustomAttribute<DataMemberAttribute>()?.Order != null)
                .ToArray();

            if (fields.Length == 0)
            {
                return new NoopWriter(type);
            }

            var boundedSizeFields = fields.Where(IsBoundedSize).ToArray();
            var varSizeFields = fields.Where(IsVarSize).ToArray();

            var boundedSize = 2 * fields.Length;
            foreach (var field in boundedSizeFields)
            {
                boundedSize += GetBoundedSize(field);
            }

            var bounded = varSizeFields.Length == 0;
            if (bounded)
            {
                return new BoundedObjectWriter(type, boundedSize, fields);
            }

            return new VarSizedObjectWriter(type, boundedSize, fields, varSizeFields);
        }

        static int GetBoundedSize(FieldInfo field) => GetBoundedSizeWriter(field).Size;
        static IVarSizeWriter GetVarSizeWriter(FieldInfo field) => (IVarSizeWriter)AllWriters.GetWriter(field);
        static IBoundedSizeWriter GetBoundedSizeWriter(FieldInfo field) => (IBoundedSizeWriter)AllWriters.GetWriter(field);

        static bool IsVarSize(FieldInfo field) => AllWriters.GetWriter(field) is IVarSizeWriter;
        static bool IsBoundedSize(FieldInfo field) => AllWriters.GetWriter(field) is IBoundedSizeWriter;

        static ushort GetManifestValue(FieldInfo field)
        {
            var order = (byte)field.GetCustomAttribute<DataMemberAttribute>().Order;
            var value = AllWriters.GetWriter(field).Manifest;

            return (ushort)(order | (value << 8));
        }

        class NoopWriter : IWriteManifest, IBoundedSizeWriter, IUpdateOffset
        {
            public NoopWriter(Type type)
            {
                Type = type;
            }

            public Type Type { get; }
            public byte Manifest => Manifests.Object;
            public bool RequiresAddress => false;
            public void EmitWrite(WriterEmitContext context)
            {
            }

            public int Size => 0;
        }

        abstract class ObjectWriter : IUpdateOffset
        {
            readonly FieldInfo[] fields;

            protected ObjectWriter(Type type, FieldInfo[] fields)
            {
                this.fields = fields;
                Type = type;
            }

            public Type Type { get; }
            public byte Manifest => 13;
            public bool RequiresAddress => Type.IsValueType;

            public void EmitWrite(WriterEmitContext context)
            {
                var il = context.Il;

                foreach (var field in fields)
                {
                    var writer = AllWriters.GetWriter(field);

                    var manifest = GetManifestValue(field);

                    void LoadValue()
                    {
                        il.Emit(writer.RequiresAddress ? OpCodes.Ldflda : OpCodes.Ldfld, field);
                    }

                    using (context.Scope(LoadValue, manifest: manifest))
                    {
                        // if the writer does not write manifest, this will write manifest for the writer
                        if (writer is IWriteManifest == false)
                        {
                            context.WriteManifest();
                        }

                        writer.EmitWrite(context);
                    }

                    if (writer is IUpdateOffset == false)
                    {
                        context.AddToCurrentOffset();
                    }
                }
            }
        }

        class BoundedObjectWriter : ObjectWriter, IBoundedSizeWriter
        {
            public BoundedObjectWriter(Type type, int size, FieldInfo[] allFields)
                : base(type, allFields)
            {
                Size = size;
            }

            public int Size { get; }
        }

        class VarSizedObjectWriter : ObjectWriter, IVarSizeWriter
        {
            readonly int size;
            readonly IEnumerable<FieldInfo> varSizeFields;

            public VarSizedObjectWriter(Type type, int size, FieldInfo[] allFields,
                IEnumerable<FieldInfo> varSizeFields) : base(type, allFields)
            {
                this.size = size;
                this.varSizeFields = varSizeFields;
            }

            public void EmitSizeEstimator(WriterEmitContext ctx)
            {
                var il = ctx.Il;

                using (ctx.GetLocal<int>(out var varSize))
                {
                    il.LoadIntValue(0);
                    il.Store(varSize);

                    foreach (var field in varSizeFields)
                    {
                        var writer = GetVarSizeWriter(field);

                        void LoadValue()
                        {
                            il.Emit(writer.RequiresAddress ? OpCodes.Ldflda : OpCodes.Ldfld, field);
                        }

                        using (ctx.Scope(LoadValue))
                        {
                            writer.EmitSizeEstimator(ctx);
                        }

                        // add to already counted bytes
                        il.Load(varSize);
                        il.Emit(OpCodes.Add);
                        il.Store(varSize);
                    }

                    il.Load(varSize);
                    il.LoadIntValue(size);
                    il.Emit(OpCodes.Add);
                }
            }
        }
    }
}
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Enzyme.Visiting;

namespace Enzyme.Writers
{
    static class AllWriters
    {
        static readonly ConcurrentDictionary<Type, IWriter> ByType;
        static readonly Dictionary<byte, IAcceptVisitor> ByManifest;

        static AllWriters()
        {
            var writers = typeof(IWriter).Assembly.GetTypes()
                .Where(t => typeof(IWriter).IsAssignableFrom(t))
                .Where(t => t.IsInterface == false && t.IsAbstract == false)
                .Where(t => t.ContainsGenericParameters == false)
                .Where(t => t.GetConstructor(Type.EmptyTypes) != null)
                .Select(Activator.CreateInstance)
                .Cast<IWriter>()
                .Select(Decorate)
                .ToArray();

            ByType = new ConcurrentDictionary<Type, IWriter>(writers.ToDictionary(writer => writer.Type));

            ByManifest = writers.Where(w => w is IAcceptVisitor).ToDictionary(w => w.Manifest, w => (IAcceptVisitor)w);
        }

        static IWriter Decorate(IWriter writer)
        {
            if (writer is IVarSizeWriter varSized)
            {
                return new VarSizedStoreLengthDecorator(varSized);
            }

            if (writer is IBoundedSizeWriter bounded)
            {
                if (writer.Manifest == Manifests.Object && bounded.Size > 0)
                {
                    return new ObjectBoundedSizedStoreLengthDecorator(bounded);
                }
            }

            return writer;
        }

        public static IAcceptVisitor GetAcceptor(byte manifest) => ByManifest[manifest];

        public static IWriter GetWriter(FieldInfo field) => GetWriter(field.FieldType);

        public static IWriter GetWriter(Type type) => ByType.GetOrAdd(type, Build);

        static IWriter Build(Type type) => Decorate(BuildImpl(type));

        static IWriter BuildImpl(Type type)
        {
            if (type.IsArray)
            {
                var elementType = type.GetElementType();
                var itemWriter = GetWriter(elementType);
                Type writerType;

                switch (itemWriter)
                {
                    case IBoundedSizeWriter _:
                        writerType = typeof(BoundedSizeArrayWriter<>).MakeGenericType(elementType);
                        break;
                    case IVarSizeWriter _:
                        writerType = typeof(VarSizeArrayWriter<>).MakeGenericType(elementType);
                        break;
                    default:
                        throw new ArgumentException($"Writer of an unknown type: {elementType}");
                }

                return (IWriter)Activator.CreateInstance(writerType, itemWriter);
            }

            var enumerable = type.GetInterfaces().FirstOrDefault(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
            if (enumerable != null)
            {
                var itemType = enumerable.GetGenericArguments()[0];
                var itemWriter = GetWriter(itemType);
                Type writerType;

                switch (itemWriter)
                {
                    case IBoundedSizeWriter _:
                        writerType = typeof(EnumerableWriter<>).MakeGenericType(itemType);
                        break;
                    case IVarSizeWriter _:
                        writerType = typeof(EnumerableWriter<>).MakeGenericType(itemType);
                        break;
                    default:
                        throw new ArgumentException($"Writer of an unknown type: {itemType}");
                }

                return (IWriter)Activator.CreateInstance(writerType, itemWriter);
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Memory<>))
            {
                var itemType = type.GetGenericArguments()[0];
                var spanWriter = GetWriter(itemType);
                var writerType = typeof(MemoryWriter<>).MakeGenericType(itemType);
                return (IWriter)Activator.CreateInstance(writerType, spanWriter);
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                var itemType = type.GetGenericArguments()[0];
                var itemWriter = GetWriter(itemType);
                var itemWriterType = itemWriter.GetType();

                var baseType = itemWriterType.BaseType;
                if (baseType != null && baseType.IsGenericType)
                {
                    var genericBase = baseType.GetGenericTypeDefinition();
                    var primitiveType = baseType.GetGenericArguments();
                    Type nullableWriterType;

                    if (genericBase == typeof(FixedPrimitiveWriter<>))
                    {
                        nullableWriterType = typeof(NullableFixedWriter<>).MakeGenericType(primitiveType);
                    }
                    else if (genericBase == typeof(BoundedPrimitiveWriter<>))
                    {
                        nullableWriterType = typeof(NullableBoundedWriter<>).MakeGenericType(primitiveType);
                    }
                    else
                    {
                        throw new ArgumentException($"Cannot handle '{type.FullName}'");
                    }

                    return (IWriter)Activator.CreateInstance(nullableWriterType, itemWriter);
                }
            }

            return ObjectWriterFactory.Build(type);
        }
    }
}
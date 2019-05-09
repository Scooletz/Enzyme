using System;
using Enzyme.Writers;

namespace Enzyme.Visiting
{
    public static class Visitor
    {
        public static unsafe void Visit<TVisitor>(ref TVisitor visitor, Memory<byte> memory)
            where TVisitor : struct, IVisitor
        {
            using (var handle = memory.Pin())
            {
                var bytes = (byte*)handle.Pointer;
                Visit(ref visitor, bytes, memory.Length);
            }
        }

        public static unsafe void Visit<TVisitor>(ref TVisitor visitor, byte* bytes, int length)
            where TVisitor : struct, IVisitor
        {
            VisitorImpl<TVisitor>.ReadObject(ref visitor, ref bytes, bytes + length);
        }

        static class VisitorImpl<TVisitor>
            where TVisitor : struct, IVisitor
        {
            public static unsafe void ReadObject(ref TVisitor v, ref byte* bytes, byte* end)
            {
                while (end - bytes > 2)
                {
                    var fieldNumber = *bytes++;
                    var manifest = *bytes++;

                    v.PushField(fieldNumber);

                    // remove nullable
                    manifest = (byte)(manifest & ~Manifests.Nullable);

                    if (Manifests.IsArray(manifest))
                    {
                        if (StoreLengthDecorator.TryReadLength(ref bytes, out var length))
                        {
                            var itemManifest = (byte)(manifest & ~Manifests.Array);
                            ReadArray(ref v, bytes, bytes + length, itemManifest);
                            bytes += length;
                        }
                        else
                        {
                            v.OnNull();
                        }

                    }
                    else if (Manifests.IsPrimitive(manifest))
                    {
                        ReadValue(ref v, ref bytes, manifest);
                    }
                    else
                    {
                        if (StoreLengthDecorator.TryReadLength(ref bytes, out var length))
                        {
                            ReadObject(ref v, ref bytes, bytes + length);
                        }
                        else
                        {
                            v.OnNull();
                        }
                    }

                    v.PopField();
                }
            }

            static unsafe void ReadValue(ref TVisitor v, ref byte* bytes, byte manifest)
            {
                var shift = AllWriters.GetAcceptor(manifest).Accept(bytes, ref v);
                bytes += shift;
            }

            static unsafe void ReadArray(ref TVisitor v, byte* bytes, byte* end, byte manifest)
            {
                while (bytes < end)
                {
                    if (Manifests.IsObject(manifest))
                    {
                        ReadObject(ref v, ref bytes, end);
                    }
                    else if (Manifests.IsPrimitive(manifest))
                    {
                        ReadValue(ref v, ref bytes, manifest);
                    }
                    else
                    {
                        throw new NotImplementedException("Nested arrays are not handled now");
                    }
                }
            }
        }
    }
}
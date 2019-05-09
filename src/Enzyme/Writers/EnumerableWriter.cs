using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Enzyme.Writers
{
    class EnumerableWriter<T> : EnumerableLikeWriter
    {
        public override Type Type => typeof(IEnumerable<T>);
        public override bool RequiresAddress => false;

        protected static readonly MethodInfo CountMethod = typeof(Enumerable).GetMethods()
            .Where(mi => mi.Name == nameof(Enumerable.Count))
            .Single(mi => mi.GetParameters().Length == 1)
            .MakeGenericMethod(typeof(T));

        public EnumerableWriter(IWriter itemWriter)
            : base(itemWriter)
        { }

        protected override void EmitCount(WriterEmitContext context)
        {
            context.LoadValue();
            context.Il.EmitCall(OpCodes.Call, CountMethod, null);
        }
    }
}
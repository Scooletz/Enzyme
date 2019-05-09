namespace Enzyme
{
    /// <summary>
    /// Provides serialization methods for concrete, attributed types.
    /// </summary>
    public static class Serializer
    {
        public static void Serialize<TContext, TValue>(ref TValue value, ref TContext context, IWriter<TContext> writer)
        {
            Holder<TContext, TValue>.Write(ref value, ref context, writer);
        }
    }

    /// <summary>
    /// Serializes the <paramref name="value"/> passing its serialized payload to <paramref name="writer"/>.
    /// </summary>
    /// <typeparam name="TContext"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="value"></param>
    /// <param name="context"></param>
    /// <param name="writer"></param>
    delegate void Write<TContext, TValue>(ref TValue value, ref TContext context, IWriter<TContext> writer);

    static class Holder<TContext, TValue>
    {
        public static readonly Write<TContext, TValue> Write;

        static Holder()
        {
            Write = Builder.Build<TContext, TValue>();
        }
    }
}
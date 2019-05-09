using System;

namespace Enzyme
{
    /// <summary>
    /// An interface for writing a serialized value to the output.
    /// </summary>
    /// <typeparam name="TContext">The context to be passed to the writer.</typeparam>
    public interface IWriter<TContext>
    {
        /// <summary>
        /// Writes the serialized value.
        /// </summary>
        /// <param name="context">The context passed by the caller of <see cref="ISerializer{TValue}.Serialize{TContext,TWriter}"/></param>
        /// <param name="payload">The payload that the value was serialized into.</param>
        void Write(ref TContext context, Span<byte> payload);
    }
}
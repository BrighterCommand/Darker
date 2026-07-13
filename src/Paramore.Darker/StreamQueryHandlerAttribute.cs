using System;

namespace Paramore.Darker
{
    /// <summary>
    /// Base attribute for stream query handler decorators. Mirrors QueryHandlerAttributeAsync for the stream pipeline.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public abstract class StreamQueryHandlerAttribute : Attribute
    {
        public int Step { get; }

        protected StreamQueryHandlerAttribute(int step)
        {
            Step = step;
        }

        public abstract object[] GetAttributeParams();

        public abstract Type GetDecoratorType();
    }
}

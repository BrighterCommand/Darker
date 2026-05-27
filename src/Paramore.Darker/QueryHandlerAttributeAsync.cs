using System;

namespace Paramore.Darker
{
    [AttributeUsage(AttributeTargets.Method)]
    public abstract class QueryHandlerAttributeAsync : Attribute
    {
        public int Step { get; private set; }

        protected QueryHandlerAttributeAsync(int step)
        {
            Step = step;
        }

        public abstract object[] GetAttributeParams();

        public abstract Type GetDecoratorType();
    }
}

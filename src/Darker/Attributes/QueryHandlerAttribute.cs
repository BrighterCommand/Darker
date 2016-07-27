using System;

namespace Darker.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public abstract class QueryHandlerAttribute : Attribute
    {
        public int Step { get; private set; }

        protected QueryHandlerAttribute(int step)
        {
            Step = step;
        }

        public abstract object[] GetAttributeParams();

        public abstract Type GetDecoratorType();
    }
}
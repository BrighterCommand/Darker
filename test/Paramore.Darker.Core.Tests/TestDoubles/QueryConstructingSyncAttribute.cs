using System;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>
    /// A sync decorator attribute naming a decorator that closes over a concrete query class only.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class QueryConstructingSyncAttribute : QueryHandlerAttribute
    {
        public QueryConstructingSyncAttribute() : base(1) { }

        public override object[] GetAttributeParams() => [];

        public override Type GetDecoratorType() => typeof(QueryConstructingDecorator<,>);
    }
}

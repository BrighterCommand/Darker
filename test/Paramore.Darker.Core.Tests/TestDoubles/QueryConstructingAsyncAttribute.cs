using System;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>
    /// A async decorator attribute naming a decorator that closes over a concrete query class only.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class QueryConstructingAsyncAttribute : QueryHandlerAttributeAsync
    {
        public QueryConstructingAsyncAttribute() : base(1) { }

        public override object[] GetAttributeParams() => [];

        public override Type GetDecoratorType() => typeof(QueryConstructingDecorator<,>);
    }
}

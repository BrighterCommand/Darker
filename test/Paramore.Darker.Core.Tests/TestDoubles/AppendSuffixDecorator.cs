using System;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>
    /// Attribute that wires <see cref="AppendSuffixDecorator{TQuery,TResult}"/> into the
    /// pipeline. Used to verify that routing still hands off to the existing decorator-discovery
    /// machinery in <c>PipelineBuilder</c>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class AppendSuffixAttribute : QueryHandlerAttribute
    {
        public AppendSuffixAttribute(int step) : base(step) { }

        public override object[] GetAttributeParams() => [];

        public override Type GetDecoratorType() => typeof(AppendSuffixDecorator<,>);
    }

    /// <summary>
    /// A decorator that appends <c>"-decorated"</c> to a string result so tests can assert
    /// the decorator pipeline was actually traversed (not bypassed by routing).
    /// </summary>
    internal sealed class AppendSuffixDecorator<TQuery, TResult> : IQueryHandlerDecorator<TQuery, TResult>
        where TQuery : IQuery<TResult>
    {
        public IQueryContext Context { get; set; }

        public void InitializeFromAttributeParams(object[] attributeParams) { }

        public TResult Execute(TQuery query, Func<TQuery, TResult> next, Func<TQuery, TResult> fallback)
        {
            var result = next(query);
            if (result is string s)
                return (TResult)(object)(s + "-decorated");
            return result;
        }
    }
}

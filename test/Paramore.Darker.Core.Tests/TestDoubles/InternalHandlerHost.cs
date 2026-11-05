namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>An internal type that hosts a public nested handler, so the handler is not visible outside the assembly.</summary>
    internal static class InternalHandlerHost
    {
        public sealed class NestedPublicHandler : QueryHandler<TestQuery, TestQuery.Result>
        {
            public override TestQuery.Result Execute(TestQuery query) => new();
        }
    }
}

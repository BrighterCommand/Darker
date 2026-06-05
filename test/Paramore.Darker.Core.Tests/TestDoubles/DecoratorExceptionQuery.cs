namespace Paramore.Darker.Core.Tests.TestDoubles
{
    internal class DecoratorExceptionQuery : IQuery<DecoratorExceptionQuery.Result>
    {
        public class Result { }
    }

    internal class DecoratorExceptionQueryHandler : QueryHandler<DecoratorExceptionQuery, DecoratorExceptionQuery.Result>
    {
        [DecoratorException(step: 1)]
        public override DecoratorExceptionQuery.Result Execute(DecoratorExceptionQuery query)
        {
            return new DecoratorExceptionQuery.Result();
        }
    }
}

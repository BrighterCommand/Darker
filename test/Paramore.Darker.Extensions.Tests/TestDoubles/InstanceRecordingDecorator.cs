using System;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>
    /// A decorator that takes an <see cref="ITrackedDependency"/> by constructor injection and, once the
    /// rest of the pipeline has run, writes its own identity and its dependency into the
    /// <see cref="SharedRecordingResult"/>.
    /// </summary>
    public sealed class InstanceRecordingDecorator<TQuery, TResult> : IQueryHandlerDecorator<TQuery, TResult>
        where TQuery : IQuery<TResult>
    {
        private readonly Guid _id = Guid.NewGuid();
        private readonly ITrackedDependency _dependency;

        public InstanceRecordingDecorator(ITrackedDependency dependency) => _dependency = dependency;

        public IQueryContext Context { get; set; }

        public void InitializeFromAttributeParams(object[] attributeParams) { }

        public TResult Execute(TQuery query, Func<TQuery, TResult> next, Func<TQuery, TResult> fallback)
        {
            var result = next(query);
            if (result is SharedRecordingResult recording)
            {
                recording.DecoratorIds.Add(_id);
                recording.DecoratorDependencies.Add(_dependency);
            }

            return result;
        }
    }
}

// Licensed under the MIT License.
// Copyright (c) .NET Foundation and Contributors.

using System.Threading;
using System.Threading.Tasks;
using Paramore.Darker.Policies.Attributes;

namespace Paramore.Darker.Tests.EndToEnd.Handlers
{
    /// <summary>
    /// Handles <see cref="RetriedQuery"/>. Throws <see cref="TransientFailureException"/> on its first
    /// call and returns <c>"recovered"</c> afterwards. Failure is decided by call count only.
    /// </summary>
    public sealed class RetriedQueryHandler : QueryHandlerAsync<RetriedQuery, string>
    {
        /// <summary>The key of the retry pipeline registered by the scenario.</summary>
        public const string RetryPipelineName = "EndToEnd.Retry";

        private readonly CallCounter _counter;

        /// <summary>Initializes a new instance of the <see cref="RetriedQueryHandler"/> class.</summary>
        /// <param name="counter">The Singleton counter of handler calls.</param>
        public RetriedQueryHandler(CallCounter counter)
        {
            _counter = counter;
        }

        /// <inheritdoc />
        [UseResiliencePipelineAttributeAsync(1, RetryPipelineName)]
        public override Task<string> ExecuteAsync(RetriedQuery query, CancellationToken cancellationToken = default)
        {
            if (_counter.Increment() == 1)
            {
                throw new TransientFailureException();
            }

            return Task.FromResult("recovered");
        }
    }
}

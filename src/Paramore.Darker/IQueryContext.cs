#nullable enable annotations
using System.Collections.Generic;
using System.Diagnostics;
using Paramore.Darker.Observability;
using Polly;
using Polly.Registry;

namespace Paramore.Darker
{
    public interface IQueryContext
    {
        IDictionary<string, object> Bag { get; set; }
        IPolicyRegistry<string> Policies { get; set; }
        ResiliencePipelineProvider<string> ResiliencePipeline { get; set; }
        ResilienceContext? ResilienceContext { get; set; }

        /// <summary>
        /// The ambient query span for the current query execution. Null when no tracer is configured.
        /// Set by <c>QueryProcessor</c> after creating the span; readable by handlers and decorators
        /// to nest child spans (e.g. database spans) under the query span.
        /// </summary>
        Activity? Span { get; set; }

        /// <summary>
        /// The tracer that created <see cref="Span"/>. Null when no tracer is configured.
        /// Handlers and the DB-span decorator use this to create child spans via
        /// <c>IAmADarkerTracer.CreateDbSpan</c>.
        /// </summary>
        IAmADarkerTracer? Tracer { get; set; }
    }
}
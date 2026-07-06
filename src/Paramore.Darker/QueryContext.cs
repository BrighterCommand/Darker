#nullable enable annotations
using System.Collections.Generic;
using System.Diagnostics;
using Paramore.Darker.Observability;
using Polly;
using Polly.Registry;

namespace Paramore.Darker
{
    public sealed class QueryContext : IQueryContext
    {
        public IDictionary<string, object> Bag { get; set; } = new Dictionary<string, object>();
        public IPolicyRegistry<string> Policies { get; set; }
        public ResiliencePipelineProvider<string> ResiliencePipeline { get; set; }
        public ResilienceContext? ResilienceContext { get; set; }
        public Activity? Span { get; set; } = null;
        public IAmADarkerTracer? Tracer { get; set; } = null;
    }
}
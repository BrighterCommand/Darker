#nullable enable annotations
using System.Collections.Generic;
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
    }
}
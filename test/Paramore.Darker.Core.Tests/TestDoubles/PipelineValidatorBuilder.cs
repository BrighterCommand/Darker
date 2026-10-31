using System.Collections.Generic;
using Paramore.Darker.PipelineValidation;
namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>
    /// Test-data builder for <see cref="PipelineValidator"/>. It is the only place that changes when the
    /// validator's constructor grows. Registries default to null, which the validator treats as empty.
    /// </summary>
    public sealed class PipelineValidatorBuilder
    {
        private IQueryHandlerRegistry? _syncRegistry;
        private IQueryHandlerRegistryAsync? _asyncRegistry;
        private IStreamQueryHandlerRegistry? _streamRegistry;
        private IEnumerable<DecoratorOrderingRule> _orderingRules = DecoratorOrderingRules.Default;
        private IAmAComponentResolvabilityProbe? _probe;

        public PipelineValidatorBuilder WithSyncRegistry(IQueryHandlerRegistry? registry)
        {
            _syncRegistry = registry;
            return this;
        }

        public PipelineValidatorBuilder WithAsyncRegistry(IQueryHandlerRegistryAsync? registry)
        {
            _asyncRegistry = registry;
            return this;
        }

        public PipelineValidatorBuilder WithStreamRegistry(IStreamQueryHandlerRegistry? registry)
        {
            _streamRegistry = registry;
            return this;
        }

        public PipelineValidatorBuilder WithProbe(IAmAComponentResolvabilityProbe? probe)
        {
            _probe = probe;
            return this;
        }

        public PipelineValidatorBuilder WithOrderingRules(IEnumerable<DecoratorOrderingRule> orderingRules)
        {
            _orderingRules = orderingRules;
            return this;
        }

        public PipelineValidator Build() => new(_syncRegistry, _asyncRegistry, _streamRegistry, _orderingRules, _probe);
    }
}

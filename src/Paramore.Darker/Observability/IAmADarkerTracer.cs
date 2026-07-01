using System;

namespace Paramore.Darker.Observability;

/// <summary>
/// Role interface for the Darker tracer. Owns the <c>ActivitySource</c> and manages the
/// query-span lifecycle. Implement this interface to provide a custom tracer, or use the
/// default <c>DarkerTracer</c> registered by <c>AddDarkerInstrumentation()</c>.
/// </summary>
public interface IAmADarkerTracer : IDisposable
{
}

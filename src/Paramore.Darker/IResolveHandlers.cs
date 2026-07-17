using System;

namespace Paramore.Darker
{
    /// <summary>
    /// The routing role: decides which handler type serves a given query execution.
    /// Every registry entry implements this role; type-based and agreement-dispatch
    /// registrations are two implementations stored side-by-side in the same dictionary.
    /// </summary>
    public interface IResolveHandlers
    {
        /// <summary>
        /// Returns the handler type that should process <paramref name="query"/> in the given
        /// <paramref name="context"/>.  Implementations may inspect both arguments (agreement
        /// dispatch) or ignore them entirely (fixed type-based routing via
        /// <see cref="FixedHandlerRoute"/>).
        /// </summary>
        Type ResolveHandlerType(IQuery query, IQueryContext context);
    }
}

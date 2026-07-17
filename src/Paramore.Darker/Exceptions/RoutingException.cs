#region Licence

/* The MIT License (MIT)
Copyright © 2026 Ian Cooper ian_hammond_cooper@yahoo.co.uk

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE. */

#endregion

using System;

namespace Paramore.Darker.Exceptions
{
    /// <summary>
    /// Thrown when an agreement-dispatch routing function fails to resolve a valid handler for a query.
    /// Distinct from <see cref="ConfigurationException"/>, which signals that no handler is registered
    /// for the query type at all. A <see langword="catch"/> on <see cref="ConfigurationException"/>
    /// will not catch this exception.
    /// </summary>
    /// <remarks>
    /// Two failure modes are represented via <see cref="Reason"/>:
    /// <list type="bullet">
    ///   <item><see cref="RoutingFailure.NoHandlerResolved"/> — the routing function returned <see langword="null"/>.</item>
    ///   <item><see cref="RoutingFailure.UnregisteredCandidate"/> — the routing function returned a type not in the registered candidate set.</item>
    /// </list>
    /// </remarks>
    public sealed class RoutingException : Exception
    {
        /// <summary>
        /// Gets the failure mode that caused this exception.
        /// </summary>
        /// <value>A <see cref="RoutingFailure"/> value identifying why routing could not resolve a handler.</value>
        public RoutingFailure Reason { get; }

        /// <summary>
        /// Initialises a new <see cref="RoutingException"/> for a routing failure.
        /// </summary>
        /// <param name="reason">The <see cref="RoutingFailure"/> that describes why the routing function failed.</param>
        /// <param name="queryType">The <see cref="Type"/> of the query for which routing failed.</param>
        /// <param name="resolvedHandlerType">
        /// The handler <see cref="Type"/> returned by the routing function, populated only for
        /// <see cref="RoutingFailure.UnregisteredCandidate"/>; <see langword="null"/> otherwise.
        /// </param>
        public RoutingException(RoutingFailure reason, Type queryType, Type? resolvedHandlerType = null)
            : base(ComposeMessage(reason, queryType, resolvedHandlerType))
        {
            Reason = reason;
        }

        private static string ComposeMessage(RoutingFailure reason, Type queryType, Type? resolvedHandlerType) =>
            reason switch
            {
                RoutingFailure.NoHandlerResolved =>
                    $"Routing function returned null for query '{queryType.Name}': no handler could be resolved.",
                RoutingFailure.UnregisteredCandidate =>
                    $"Routing function returned '{resolvedHandlerType?.Name}' for query '{queryType.Name}', but that type is not a registered candidate.",
                _ => $"Routing failed for query '{queryType.Name}'."
            };
    }
}

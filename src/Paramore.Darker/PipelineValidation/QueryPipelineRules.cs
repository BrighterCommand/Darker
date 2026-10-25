#region Licence
/* The MIT License (MIT)
Copyright © 2026 Ian Cooper <ian_hammond_cooper@yahoo.co.uk>

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

#nullable enable

using System.Collections.Generic;
using Paramore.Darker.Specifications;

namespace Paramore.Darker.PipelineValidation;

/// <summary>
/// Supplies the rules that judge a <see cref="HandlerPipelineDescription"/>.
/// </summary>
/// <remarks>
/// A <see cref="Specification{T}"/> keeps the results of its last evaluation, so each call to
/// <see cref="Create"/> returns fresh specifications. Every error factory is null-safe and does not throw.
/// </remarks>
public static class QueryPipelineRules
{
    /// <summary>The identifier of the rule that requires a registration to name a handler.</summary>
    public static readonly ValidationRuleId HandlerRegisteredId = new("HandlerRegistered");

    /// <summary>The identifier of the rule that requires a handler type to be public.</summary>
    public static readonly ValidationRuleId HandlerPublicId = new("HandlerPublic");

    /// <summary>Creates a fresh set of pipeline rules.</summary>
    /// <returns>The rules, each a new specification.</returns>
    public static IReadOnlyList<ISpecification<HandlerPipelineDescription>> Create() =>
        [HandlerRegistered(), HandlerPublic()];

    private static Specification<HandlerPipelineDescription> HandlerRegistered() =>
        new(
            description => description.HandlerType is not null,
            description => new ValidationError(
                ValidationSeverity.Error,
                $"Query '{description.QueryType?.Name}'",
                $"No handler type is registered for query '{description.QueryType?.Name}' in the registry path '{description.Path}'.")
            {
                RuleId = HandlerRegisteredId
            });

    private static Specification<HandlerPipelineDescription> HandlerPublic() =>
        new(
            description => description.HandlerType!.IsVisible,
            description => new ValidationError(
                ValidationSeverity.Error,
                $"Handler '{description.HandlerType?.Name}'",
                $"Handler '{description.HandlerType?.Name}' is not public. Make it public, because a handler is the public facade of a module.")
            {
                RuleId = HandlerPublicId
            });
}

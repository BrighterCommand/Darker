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
using Paramore.Darker.Policies.Handlers;
using Paramore.Darker.Specifications;

namespace Paramore.Darker.PipelineValidation;

/// <summary>
/// Holds the core table of decorator ordering constraints.
/// </summary>
public static class DecoratorOrderingRules
{
    private const string FALLBACK_INSIDE_RETRY =
        "the inner Fallback catches the failure, so the outer Retry never sees an exception and never retries. Give the Fallback a lower Step than the Retry.";

    /// <summary>The core table, for the sync and async paths: a Fallback must wrap a Retry, because an inner Fallback hides the failure from the Retry.</summary>
    public static readonly IReadOnlyList<DecoratorOrderingRule> Default =
    [
        new(
            typeof(FallbackPolicyDecorator<,>),
            typeof(RetryableQueryDecorator<,>),
            PipelineRuleIds.DecoratorOrdering,
            FALLBACK_INSIDE_RETRY),
        new(
            typeof(FallbackPolicyDecoratorAsync<,>),
            typeof(RetryableQueryDecoratorAsync<,>),
            PipelineRuleIds.DecoratorOrdering,
            FALLBACK_INSIDE_RETRY)
    ];
}

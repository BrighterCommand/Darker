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

using System;
using System.Linq;
using Paramore.Darker.Specifications;

namespace Paramore.Darker.PipelineValidation;

/// <summary>
/// States that one decorator type must wrap another: <see cref="Outer"/> must run outside <see cref="Inner"/>.
/// </summary>
/// <param name="Outer">The open generic decorator type that must be the outer one.</param>
/// <param name="Inner">The open generic decorator type that must be the inner one.</param>
/// <param name="RuleId">The identifier reported on a finding when the pair is in the wrong order.</param>
/// <param name="Reason">Why the order matters, reported in the finding's message.</param>
public sealed record DecoratorOrderingRule(Type Outer, Type Inner, ValidationRuleId RuleId, string Reason)
{
    // Steps come in builder order: the first is closest to the handler and the last is outermost.
    internal bool IsViolatedBy(HandlerPipelineDescription description)
    {
        var positions = description.Steps.Select(step => step.DecoratorType).ToList();
        var outerPosition = positions.FindIndex(type => IsOpenGenericOf(type, Outer));
        var innerPosition = positions.FindLastIndex(type => IsOpenGenericOf(type, Inner));
        return outerPosition >= 0 && innerPosition > outerPosition;
    }

    private static bool IsOpenGenericOf(Type type, Type openType) =>
        type == openType || (type.IsGenericType && type.GetGenericTypeDefinition() == openType);
}

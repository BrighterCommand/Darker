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

using Paramore.Darker.Specifications;

namespace Paramore.Darker.PipelineValidation;

/// <summary>
/// The identifiers of the rules that validate query pipelines.
/// </summary>
public static class PipelineRuleIds
{
    /// <summary>The rule that requires a registration to name a handler.</summary>
    public static readonly ValidationRuleId HandlerRegistered = new("HandlerRegistered");

    /// <summary>The rule that requires a handler type to be public.</summary>
    public static readonly ValidationRuleId HandlerPublic = new("HandlerPublic");

    /// <summary>The rule that requires a handler to implement the interface of its registry's path.</summary>
    public static readonly ValidationRuleId HandlerMatchesPath = new("HandlerMatchesPath");

    /// <summary>The rule that forbids a path's method to carry another path's decorator attributes.</summary>
    public static readonly ValidationRuleId DecoratorAttributeKind = new("DecoratorAttributeKind");

    /// <summary>The rule that requires a handler's pipeline to be describable.</summary>
    public static readonly ValidationRuleId PipelineDescribable = new("PipelineDescribable");

    /// <summary>The rule that warns when two attributes of a path share a step.</summary>
    public static readonly ValidationRuleId DuplicateStep = new("DuplicateStep");

    /// <summary>The rule that requires a handler type to be registered, according to the resolvability probe.</summary>
    public static readonly ValidationRuleId HandlerResolvable = new("HandlerResolvable");

    /// <summary>The rule that requires every decorator type of a pipeline to be registered, according to the resolvability probe.</summary>
    public static readonly ValidationRuleId DecoratorResolvable = new("DecoratorResolvable");

    /// <summary>The rule that warns when two decorators of a <see cref="DecoratorOrderingRule"/> are in the wrong order.</summary>
    public static readonly ValidationRuleId DecoratorOrdering = new("DecoratorOrdering");

    /// <summary>The warning that the resolvability rules were skipped because the validator has no probe.</summary>
    public static readonly ValidationRuleId ResolvabilityNotChecked = new("ResolvabilityNotChecked");

    /// <summary>The warning for a registry that cannot be inspected.</summary>
    public static readonly ValidationRuleId RegistryNotInspectable = new("RegistryNotInspectable");
}

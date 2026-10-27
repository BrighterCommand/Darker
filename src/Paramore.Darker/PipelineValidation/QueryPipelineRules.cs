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
using System.Collections.Generic;
using System.Linq;
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
    /// <summary>Creates a fresh set of pipeline rules.</summary>
    /// <param name="orderingRules">The decorator ordering table. Each entry becomes its own rule.</param>
    /// <param name="probe">The probe that answers whether a component type is registered. The resolvability rules are included only when it is supplied.</param>
    /// <returns>The rules, each a new specification.</returns>
    public static IReadOnlyList<ISpecification<HandlerPipelineDescription>> Create(IEnumerable<DecoratorOrderingRule> orderingRules, IAmAComponentResolvabilityProbe? probe = null)
    {
        List<ISpecification<HandlerPipelineDescription>> rules =
            [HandlerRegistered(), HandlerPublic(), HandlerMatchesPath(), DecoratorAttributeKind(), PipelineDescribable(), DuplicateStep()];

        rules.AddRange(orderingRules.Select(DecoratorOrdering));

        if (probe is not null)
        {
            rules.Add(HandlerResolvable(probe));
            rules.Add(DecoratorResolvable(probe));
        }

        return rules;
    }

    private static Specification<HandlerPipelineDescription> DecoratorOrdering(DecoratorOrderingRule orderingRule) =>
        new(
            description => !IsOutOfOrder(description, orderingRule),
            description => new ValidationError(
                ValidationSeverity.Warning,
                HandlerSource(description),
                $"{HandlerSource(description)} {Where(description)} has {OpenName(orderingRule.Inner)} outside {OpenName(orderingRule.Outer)}, but {OpenName(orderingRule.Outer)} must wrap {OpenName(orderingRule.Inner)}: {orderingRule.Reason}")
            {
                RuleId = orderingRule.RuleId
            });

    // Steps come in builder order: the first is closest to the handler and the last is outermost.
    private static bool IsOutOfOrder(HandlerPipelineDescription description, DecoratorOrderingRule orderingRule)
    {
        var positions = description.Steps.Select(step => step.DecoratorType).ToList();
        var outerPosition = positions.FindIndex(type => IsOpenGenericOf(type, orderingRule.Outer));
        var innerPosition = positions.FindLastIndex(type => IsOpenGenericOf(type, orderingRule.Inner));
        return outerPosition >= 0 && innerPosition > outerPosition;
    }

    private static bool IsOpenGenericOf(Type type, Type openType) =>
        type == openType || (type.IsGenericType && type.GetGenericTypeDefinition() == openType);

    private static string OpenName(Type? type) => $"'{type?.Name.Split('`')[0]}'";

    private static Specification<HandlerPipelineDescription> HandlerResolvable(IAmAComponentResolvabilityProbe probe) =>
        new(
            description => description.HandlerType is null || probe.Resolves(description.HandlerType),
            description => new ValidationError(
                ValidationSeverity.Error,
                HandlerSource(description),
                $"{HandlerSource(description)} {Where(description)} is not registered. Register it with the container.")
            {
                RuleId = PipelineRuleIds.HandlerResolvable
            });

    private static Specification<HandlerPipelineDescription> DecoratorResolvable(IAmAComponentResolvabilityProbe probe) =>
        new(
            description => description.Steps.All(step => probe.Resolves(step.DecoratorType)),
            description => new ValidationError(
                ValidationSeverity.Error,
                HandlerSource(description),
                $"{HandlerSource(description)} {Where(description)} uses {UnresolvedDecorators(description, probe)}, which is not registered. Register it with the container.")
            {
                RuleId = PipelineRuleIds.DecoratorResolvable
            });

    private static string UnresolvedDecorators(HandlerPipelineDescription description, IAmAComponentResolvabilityProbe probe)
    {
        var decorators = description.Steps.Select(step => step?.DecoratorType).Where(type => type is not null).ToList();
        try
        {
            decorators = decorators.Where(type => !probe.Resolves(type!)).ToList();
        }
        catch (Exception)
        {
            // the probe failed again, so name every decorator of the pipeline rather than throw from the error factory
        }

        return string.Join(", ", decorators.Select(type => $"'{type!.Name}'"));
    }

    private static Specification<HandlerPipelineDescription> HandlerRegistered() =>
        ErrorRule(
            PipelineRuleIds.HandlerRegistered,
            description => description.HandlerType is not null,
            description => $"Query '{description.QueryType?.Name}'",
            description => $"No handler type is registered {Where(description)}.");

    private static Specification<HandlerPipelineDescription> HandlerPublic() =>
        ErrorRule(
            PipelineRuleIds.HandlerPublic,
            description => description.HandlerType is null || description.HandlerType.IsVisible,
            HandlerSource,
            description => $"{HandlerSource(description)} is not public. Make it public, because a handler is the public facade of a module.");

    private static Specification<HandlerPipelineDescription> HandlerMatchesPath() =>
        ErrorRule(
            PipelineRuleIds.HandlerMatchesPath,
            description => description.HandlerType is null || (ImplementsPathInterface(description) && description.HasPathMethod),
            HandlerSource,
            HandlerMatchesPathMessage);

    private static Specification<HandlerPipelineDescription> DecoratorAttributeKind() =>
        ErrorRule(
            PipelineRuleIds.DecoratorAttributeKind,
            description => description.MisplacedAttributes.Count == 0,
            HandlerSource,
            DecoratorAttributeKindMessage);

    private static Specification<HandlerPipelineDescription> PipelineDescribable() =>
        ErrorRule(
            PipelineRuleIds.PipelineDescribable,
            description => description.Problems.Count == 0,
            HandlerSource,
            PipelineDescribableMessage);

    private static Specification<HandlerPipelineDescription> DuplicateStep() =>
        new((Func<HandlerPipelineDescription, IEnumerable<ValidationResult>>)(description => DuplicateSteps(description)
            .Select(group => ValidationResult.Fail(new ValidationError(
                ValidationSeverity.Warning,
                HandlerSource(description),
                DuplicateStepMessage(description, group))
            {
                RuleId = PipelineRuleIds.DuplicateStep
            }))));

    private static Specification<HandlerPipelineDescription> ErrorRule(
        ValidationRuleId ruleId,
        Func<HandlerPipelineDescription, bool> isSatisfied,
        Func<HandlerPipelineDescription, string> source,
        Func<HandlerPipelineDescription, string> message) =>
        new(
            isSatisfied,
            description => new ValidationError(ValidationSeverity.Error, source(description), message(description))
            {
                RuleId = ruleId
            });

    private static string HandlerSource(HandlerPipelineDescription description) => $"Handler '{description.HandlerType?.Name}'";

    private static string Where(HandlerPipelineDescription description) =>
        $"for query '{description.QueryType?.Name}' in the registry path '{description.Path}'";

    private static IEnumerable<IGrouping<int, PipelineStepDescription>> DuplicateSteps(HandlerPipelineDescription description) =>
        description.Steps.GroupBy(step => step.Step).Where(group => group.Count() > 1);

    private static string DuplicateStepMessage(HandlerPipelineDescription description, IGrouping<int, PipelineStepDescription> group)
    {
        var attributes = string.Join(", ", group.Select(step => $"'{step.AttributeType?.Name}'"));
        return $"{HandlerSource(description)} {Where(description)} has {attributes} at the same Step {group.Key}, so their order is undefined. Give each attribute its own Step.";
    }

    private static string PipelineDescribableMessage(HandlerPipelineDescription description)
    {
        var problems = string.Join(" ", description.Problems.Select(problem => $"'{problem.Subject}' could not be read: {problem.Message}"));
        return $"The pipeline of handler '{description.HandlerType?.Name}' {Where(description)} cannot be described. {problems}";
    }

    private static string DecoratorAttributeKindMessage(HandlerPipelineDescription description)
    {
        var attributes = string.Join(", ", description.MisplacedAttributes.Select(attributeType => $"'{attributeType?.Name}'"));
        var correctKind = PipelinePathReader.For(description.Path)?.AttributeKind.Name;
        return $"{HandlerSource(description)} {Where(description)} carries {attributes} on its method, which belongs to another path. Use '{correctKind}' instead.";
    }

    private static string HandlerMatchesPathMessage(HandlerPipelineDescription description)
    {
        var interfaceName = PipelinePathReader.For(description.Path)?.PathInterface.Name.Replace("`2", "<,>");
        var prefix = $"{HandlerSource(description)} {Where(description)}";
        return description.HandlerType is not null && ImplementsPathInterface(description)
            ? $"{prefix} implements '{interfaceName}' only explicitly, so it has no public method for the path. Implement the method publicly."
            : $"{prefix} must implement '{interfaceName}'.";
    }

    private static bool ImplementsPathInterface(HandlerPipelineDescription description)
    {
        var pathInterface = PipelinePathReader.For(description.Path)?.PathInterface;
        return description.HandlerType!.GetInterfaces().Any(i =>
            i.IsGenericType
            && i.GetGenericTypeDefinition() == pathInterface
            && i.GenericTypeArguments[0] == description.QueryType
            && i.GenericTypeArguments[1] == description.ResultType);
    }
}

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
    /// <summary>The identifier of the rule that requires a registration to name a handler.</summary>
    public static readonly ValidationRuleId HandlerRegisteredId = new("HandlerRegistered");

    /// <summary>The identifier of the rule that requires a handler type to be public.</summary>
    public static readonly ValidationRuleId HandlerPublicId = new("HandlerPublic");

    /// <summary>The identifier of the rule that requires a handler to implement the interface of its registry's path.</summary>
    public static readonly ValidationRuleId HandlerMatchesPathId = new("HandlerMatchesPath");

    /// <summary>The identifier of the rule that forbids a path's method to carry another path's decorator attributes.</summary>
    public static readonly ValidationRuleId DecoratorAttributeKindId = new("DecoratorAttributeKind");

    /// <summary>The identifier of the rule that requires a handler's pipeline to be describable.</summary>
    public static readonly ValidationRuleId PipelineDescribableId = new("PipelineDescribable");

    /// <summary>The identifier of the rule that warns when two attributes of a path share a step.</summary>
    public static readonly ValidationRuleId DuplicateStepId = new("DuplicateStep");

    /// <summary>Creates a fresh set of pipeline rules.</summary>
    /// <returns>The rules, each a new specification.</returns>
    public static IReadOnlyList<ISpecification<HandlerPipelineDescription>> Create() =>
        [HandlerRegistered(), HandlerPublic(), HandlerMatchesPath(), DecoratorAttributeKind(), PipelineDescribable(), DuplicateStep()];

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
            description => description.HandlerType is null || description.HandlerType.IsVisible,
            description => new ValidationError(
                ValidationSeverity.Error,
                $"Handler '{description.HandlerType?.Name}'",
                $"Handler '{description.HandlerType?.Name}' is not public. Make it public, because a handler is the public facade of a module.")
            {
                RuleId = HandlerPublicId
            });

    private static Specification<HandlerPipelineDescription> HandlerMatchesPath() =>
        new(
            description => description.HandlerType is null || (ImplementsPathInterface(description) && description.HasPathMethod),
            description => new ValidationError(
                ValidationSeverity.Error,
                $"Handler '{description.HandlerType?.Name}'",
                HandlerMatchesPathMessage(description))
            {
                RuleId = HandlerMatchesPathId
            });

    private static Specification<HandlerPipelineDescription> DecoratorAttributeKind() =>
        new(
            description => description.MisplacedAttributes is null || description.MisplacedAttributes.Count == 0,
            description => new ValidationError(
                ValidationSeverity.Error,
                $"Handler '{description.HandlerType?.Name}'",
                DecoratorAttributeKindMessage(description))
            {
                RuleId = DecoratorAttributeKindId
            });

    private static Specification<HandlerPipelineDescription> PipelineDescribable() =>
        new(
            description => description.Problems is null || description.Problems.Count == 0,
            description => new ValidationError(
                ValidationSeverity.Error,
                $"Handler '{description.HandlerType?.Name}'",
                PipelineDescribableMessage(description))
            {
                RuleId = PipelineDescribableId
            });

    private static Specification<HandlerPipelineDescription> DuplicateStep() =>
        new((Func<HandlerPipelineDescription, IEnumerable<ValidationResult>>)(description => DuplicateSteps(description)
            .Select(group => ValidationResult.Fail(new ValidationError(
                ValidationSeverity.Warning,
                $"Handler '{description.HandlerType?.Name}'",
                DuplicateStepMessage(description, group))
            {
                RuleId = DuplicateStepId
            }))));

    private static IEnumerable<IGrouping<int, PipelineStepDescription>> DuplicateSteps(HandlerPipelineDescription description) =>
        (description.Steps ?? []).GroupBy(step => step.Step).Where(group => group.Count() > 1);

    private static string DuplicateStepMessage(HandlerPipelineDescription description, IGrouping<int, PipelineStepDescription> group)
    {
        var attributes = string.Join(", ", group.Select(step => $"'{step.AttributeType?.Name}'"));
        return $"Handler '{description.HandlerType?.Name}' for query '{description.QueryType?.Name}' in the registry path '{description.Path}' has {attributes} at the same Step {group.Key}, so their order is undefined. Give each attribute its own Step.";
    }

    private static string PipelineDescribableMessage(HandlerPipelineDescription description)
    {
        var problems = string.Join(" ", (description.Problems ?? []).Select(problem => $"'{problem.Subject}' could not be read: {problem.Message}"));
        return $"The pipeline of handler '{description.HandlerType?.Name}' for query '{description.QueryType?.Name}' in the registry path '{description.Path}' cannot be described. {problems}";
    }

    private static string DecoratorAttributeKindMessage(HandlerPipelineDescription description)
    {
        var misplaced = description.MisplacedAttributes ?? [];
        var attributes = string.Join(", ", misplaced.Select(attribute => $"'{attribute.AttributeType?.Name}'"));
        var correctKind = misplaced.FirstOrDefault()?.CorrectKind?.Name;
        return $"Handler '{description.HandlerType?.Name}' for query '{description.QueryType?.Name}' in the registry path '{description.Path}' carries {attributes} on its method, which belongs to another path. Use '{correctKind}' instead.";
    }

    private static string HandlerMatchesPathMessage(HandlerPipelineDescription description)
    {
        var interfaceName = PathInterface(description.Path)?.Name.Replace("`2", "<,>");
        var prefix = $"Handler '{description.HandlerType?.Name}' for query '{description.QueryType?.Name}' in the registry path '{description.Path}'";
        return description.HandlerType is not null && ImplementsPathInterface(description)
            ? $"{prefix} implements '{interfaceName}' only explicitly, so it has no public method for the path. Implement the method publicly."
            : $"{prefix} must implement '{interfaceName}'.";
    }

    private static bool ImplementsPathInterface(HandlerPipelineDescription description)
    {
        var pathInterface = PathInterface(description.Path);
        return description.HandlerType!.GetInterfaces().Any(i =>
            i.IsGenericType
            && i.GetGenericTypeDefinition() == pathInterface
            && i.GenericTypeArguments[0] == description.QueryType
            && i.GenericTypeArguments[1] == description.ResultType);
    }

    private static Type? PathInterface(PipelinePath path) => path switch
    {
        PipelinePath.Sync => typeof(IQueryHandler<,>),
        PipelinePath.Async => typeof(IQueryHandlerAsync<,>),
        PipelinePath.Stream => typeof(IStreamQueryHandler<,>),
        _ => null
    };
}

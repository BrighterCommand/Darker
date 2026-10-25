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
/// Validates the pipelines held by the sync, async and stream handler registries.
/// </summary>
public sealed class PipelineValidator : IAmAPipelineValidator
{
    /// <summary>The identifier of the warning for a registry that cannot be inspected.</summary>
    public static readonly ValidationRuleId RegistryNotInspectableId = new("RegistryNotInspectable");

    private readonly (PipelinePathReader Reader, object? Registry)[] _registries;

    /// <summary>Initializes a new instance of the <see cref="PipelineValidator"/> class.</summary>
    /// <param name="syncRegistry">The sync registry, or null when there is none.</param>
    /// <param name="asyncRegistry">The async registry, or null when there is none.</param>
    /// <param name="streamRegistry">The stream registry, or null when there is none.</param>
    public PipelineValidator(
        IQueryHandlerRegistry? syncRegistry,
        IQueryHandlerRegistryAsync? asyncRegistry,
        IStreamQueryHandlerRegistry? streamRegistry)
    {
        _registries = [(PipelinePathReader.Sync, syncRegistry), (PipelinePathReader.Async, asyncRegistry), (PipelinePathReader.Stream, streamRegistry)];
    }

    /// <inheritdoc />
    public PipelineValidationResult Validate()
    {
        var rules = QueryPipelineRules.Create();
        var collector = new ValidationResultCollector<HandlerPipelineDescription>();

        var findings = _registries
            .SelectMany(entry => ValidateRegistry(entry.Reader, entry.Registry, rules, collector))
            .ToList();

        return new PipelineValidationResult(
            findings.Where(finding => finding.Severity == ValidationSeverity.Error),
            findings.Where(finding => finding.Severity != ValidationSeverity.Error));
    }

    private static IReadOnlyList<ValidationError> ValidateRegistry(
        PipelinePathReader reader,
        object? registry,
        IReadOnlyList<ISpecification<HandlerPipelineDescription>> rules,
        ValidationResultCollector<HandlerPipelineDescription> collector)
    {
        if (registry is null)
            return [];

        if (registry is not IAmAHandlerRegistryInspector inspector)
            return [NotInspectable(registry)];

        var fault = TryDescribe(inspector, reader, out var descriptions);
        return fault is not null
            ? [fault]
            : descriptions.SelectMany(description => Evaluate(rules, collector, description)).ToList();
    }

    private static ValidationError NotInspectable(object registry) =>
        new(
            ValidationSeverity.Warning,
            $"Registry '{registry.GetType().Name}'",
            $"The registry '{registry.GetType().Name}' does not implement IAmAHandlerRegistryInspector, so its handlers cannot be checked.")
        {
            RuleId = RegistryNotInspectableId
        };

    private static IReadOnlyList<ValidationError> Evaluate(
        IReadOnlyList<ISpecification<HandlerPipelineDescription>> rules,
        ValidationResultCollector<HandlerPipelineDescription> collector,
        HandlerPipelineDescription description) =>
        rules
            .Where(rule => !rule.IsSatisfiedBy(description))
            .SelectMany(rule => rule.Accept(collector))
            .Where(result => !result.Success && result.Error is not null)
            .Select(result => result.Error!)
            .ToList();

    private static ValidationError? TryDescribe(
        IAmAHandlerRegistryInspector inspector,
        PipelinePathReader reader,
        out IReadOnlyList<HandlerPipelineDescription> descriptions)
    {
        try
        {
            descriptions = PipelineDescriber.Describe(inspector, reader);
            return null;
        }
        catch (Exception e)
        {
            descriptions = [];
            return new ValidationError(
                ValidationSeverity.Error,
                $"Registry '{inspector.GetType().Name}'",
                $"The registry could not be read: {e.Message}")
            {
                RuleId = ValidationRuleIds.EvaluationFailed
            };
        }
    }
}

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
using Paramore.Darker.Specifications;

namespace Paramore.Darker.PipelineValidation;

/// <summary>
/// Validates the pipelines held by the sync, async and stream handler registries.
/// </summary>
public sealed class PipelineValidator : IAmAPipelineValidator
{
    private readonly object?[] _registries;

    /// <summary>Initializes a new instance of the <see cref="PipelineValidator"/> class.</summary>
    /// <param name="syncRegistry">The sync registry, or null when there is none.</param>
    /// <param name="asyncRegistry">The async registry, or null when there is none.</param>
    /// <param name="streamRegistry">The stream registry, or null when there is none.</param>
    public PipelineValidator(
        IQueryHandlerRegistry? syncRegistry,
        IQueryHandlerRegistryAsync? asyncRegistry,
        IStreamQueryHandlerRegistry? streamRegistry)
    {
        _registries = [syncRegistry, asyncRegistry, streamRegistry];
    }

    /// <inheritdoc />
    public PipelineValidationResult Validate()
    {
        var errors = new List<ValidationError>();
        var warnings = new List<ValidationError>();
        var rules = QueryPipelineRules.Create();
        var collector = new ValidationResultCollector<HandlerPipelineDescription>();

        for (var index = 0; index < _registries.Length; index++)
        {
            if (_registries[index] is not IAmAHandlerRegistryInspector inspector)
                continue;

            var fault = TryDescribe(inspector, (PipelinePath)index, out var descriptions);
            if (fault is not null)
            {
                errors.Add(fault);
                continue;
            }

            foreach (var description in descriptions)
                Evaluate(rules, collector, description, errors, warnings);
        }

        return new PipelineValidationResult(errors, warnings);
    }

    private static void Evaluate(
        IReadOnlyList<ISpecification<HandlerPipelineDescription>> rules,
        ValidationResultCollector<HandlerPipelineDescription> collector,
        HandlerPipelineDescription description,
        List<ValidationError> errors,
        List<ValidationError> warnings)
    {
        foreach (var rule in rules)
        {
            if (rule.IsSatisfiedBy(description))
                continue;

            foreach (var result in rule.Accept(collector))
            {
                if (result.Success || result.Error is null)
                    continue;

                (result.Error.Severity == ValidationSeverity.Error ? errors : warnings).Add(result.Error);
            }
        }
    }

    private static ValidationError? TryDescribe(
        IAmAHandlerRegistryInspector inspector,
        PipelinePath path,
        out IReadOnlyList<HandlerPipelineDescription> descriptions)
    {
        try
        {
            descriptions = PipelineDescriber.Describe(inspector, path);
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

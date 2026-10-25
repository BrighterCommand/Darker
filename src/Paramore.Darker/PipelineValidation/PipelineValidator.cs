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

        foreach (var registry in _registries)
        {
            if (registry is not IAmAHandlerRegistryInspector inspector)
                continue;

            var fault = TryReadRegistrations(inspector);
            if (fault is not null)
                errors.Add(fault);
        }

        return new PipelineValidationResult(errors, []);
    }

    private static ValidationError? TryReadRegistrations(IAmAHandlerRegistryInspector inspector)
    {
        try
        {
            inspector.GetRegistrations();
            return null;
        }
        catch (Exception e)
        {
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

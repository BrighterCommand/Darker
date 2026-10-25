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
using System.Linq;
using Paramore.Darker.Specifications;

namespace Paramore.Darker.PipelineValidation;

/// <summary>
/// The findings of a pipeline validation, split by severity.
/// </summary>
public sealed class PipelineValidationResult
{
    /// <summary>Gets the findings that make the configuration invalid.</summary>
    public IReadOnlyList<ValidationError> Errors { get; }

    /// <summary>Gets the findings that do not block start-up.</summary>
    public IReadOnlyList<ValidationError> Warnings { get; }

    /// <summary>Gets a value indicating whether there are no errors.</summary>
    public bool IsValid => Errors.Count == 0;

    internal PipelineValidationResult(IEnumerable<ValidationError> errors, IEnumerable<ValidationError> warnings)
    {
        Errors = errors.ToArray();
        Warnings = warnings.ToArray();
    }
}

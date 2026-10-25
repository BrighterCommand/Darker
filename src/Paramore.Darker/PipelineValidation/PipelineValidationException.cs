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
/// Thrown when pipeline validation finds errors. The message lists the error count and every finding.
/// </summary>
public sealed class PipelineValidationException : Exception
{
    /// <summary>Gets the result that carries every finding.</summary>
    public PipelineValidationResult Result { get; }

    /// <summary>Initializes a new instance of the <see cref="PipelineValidationException"/> class.</summary>
    /// <param name="result">The invalid result.</param>
    public PipelineValidationException(PipelineValidationResult result)
        : base(BuildMessage(result))
    {
        Result = result;
    }

    private static string BuildMessage(PipelineValidationResult result)
    {
        var lines = new List<string> { $"Pipeline validation failed with {result.Errors.Count} error(s):" };
        lines.AddRange(result.Errors.Concat(result.Warnings).Select(Describe));
        return string.Join(Environment.NewLine, lines);
    }

    private static string Describe(ValidationError finding) =>
        $"[{finding.RuleId}] {finding.Source}: {finding.Message}";
}

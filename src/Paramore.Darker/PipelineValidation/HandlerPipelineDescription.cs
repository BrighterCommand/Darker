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

namespace Paramore.Darker.PipelineValidation;

/// <summary>
/// Describes the pipeline of one registration, as the pipeline rules see it.
/// </summary>
/// <param name="Path">The registry the registration was found in.</param>
/// <param name="QueryType">The registered query type.</param>
/// <param name="ResultType">The query's result type.</param>
/// <param name="HandlerType">The handler type, or null when the registration has none.</param>
/// <param name="HasPathMethod">False when the handler has no public method for the path, for example because it implements the interface explicitly.</param>
/// <param name="MisplacedAttributes">The decorator attributes on the path's method that belong to another path; empty when there are none.</param>
/// <param name="Problems">The problems that stopped the pipeline being described; empty when there are none.</param>
/// <param name="Steps">The decorators the path's attributes closed, in the order the builder applies them; empty when there are none.</param>
public sealed record HandlerPipelineDescription(
    PipelinePath Path,
    Type QueryType,
    Type ResultType,
    Type? HandlerType,
    bool HasPathMethod,
    IReadOnlyList<MisplacedAttribute> MisplacedAttributes,
    IReadOnlyList<DescriptionProblem> Problems,
    IReadOnlyList<PipelineStepDescription> Steps)
{
    /// <summary>Describes a registration that names no handler type, so it has no pipeline to read.</summary>
    /// <param name="path">The registry the registration was found in.</param>
    /// <param name="queryType">The registered query type.</param>
    /// <param name="resultType">The query's result type.</param>
    /// <returns>A description with no handler type and nothing read.</returns>
    public static HandlerPipelineDescription WithoutHandler(PipelinePath path, Type queryType, Type resultType) =>
        new(path, queryType, resultType, null, true, [], [], []);
}

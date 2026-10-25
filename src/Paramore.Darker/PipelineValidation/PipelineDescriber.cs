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
using System.Reflection;

namespace Paramore.Darker.PipelineValidation;

/// <summary>
/// Describes the pipelines of the registrations a registry reports.
/// </summary>
internal static class PipelineDescriber
{
    /// <summary>Describes every candidate of every registration the inspector reports.</summary>
    /// <param name="inspector">The registry to describe.</param>
    /// <param name="reader">The reader for the path the registry serves.</param>
    /// <returns>One description for each candidate handler of each registration, in registration order.</returns>
    public static IReadOnlyList<HandlerPipelineDescription> Describe(IAmAHandlerRegistryInspector inspector, PipelinePathReader reader)
    {
        var descriptions = new List<HandlerPipelineDescription>();

        foreach (var registration in inspector.GetRegistrations())
        {
            if (registration.CandidateHandlerTypes.Count == 0)
            {
                descriptions.Add(new HandlerPipelineDescription(reader.Path, registration.QueryType, registration.ResultType, null));
                continue;
            }

            foreach (var candidate in registration.CandidateHandlerTypes)
                descriptions.Add(DescribeCandidate(candidate, registration, reader));
        }

        return descriptions;
    }

    private static HandlerPipelineDescription DescribeCandidate(Type? handlerType, HandlerRegistration registration, PipelinePathReader reader)
    {
        if (handlerType is null)
            return new HandlerPipelineDescription(reader.Path, registration.QueryType, registration.ResultType, null);

        var lookup = reader.FindMethod(handlerType, registration.QueryType);

        // A failed lookup (for example an ambiguous method) is not an absent method; it is a description problem.
        var hasPathMethod = lookup.Failure is not null || lookup.Method is not null;
        var read = ReadPipeline(lookup, registration, reader);
        return new HandlerPipelineDescription(reader.Path, registration.QueryType, registration.ResultType, handlerType, hasPathMethod, MisplacedAttributes(lookup.Method, reader), read.Problems, read.Steps);
    }

    private static PipelineRead ReadPipeline(ExecuteMethodLookup lookup, HandlerRegistration registration, PipelinePathReader reader)
    {
        if (lookup.Failure is not null)
            return new PipelineRead([new DescriptionProblem(reader.MethodName, lookup.Failure.Exception.Message)], []);

        return lookup.Method is null ? new PipelineRead([], []) : reader.ReadDecorators(lookup.Method, registration);
    }

    private static IReadOnlyList<MisplacedAttribute> MisplacedAttributes(MethodInfo? method, PipelinePathReader reader)
    {
        if (method is null) return [];

        var check = PipelineAttributeReader.CheckAttributeKinds(method, reader.HandlerPath);
        return check.AttributeTypes.Select(attributeType => new MisplacedAttribute(attributeType, reader.AttributeKind)).ToList();
    }
}

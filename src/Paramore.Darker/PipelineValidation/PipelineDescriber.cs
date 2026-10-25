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
    /// <param name="path">The path the registry serves.</param>
    /// <returns>One description for each candidate handler of each registration, in registration order.</returns>
    public static IReadOnlyList<HandlerPipelineDescription> Describe(IAmAHandlerRegistryInspector inspector, PipelinePath path)
    {
        var descriptions = new List<HandlerPipelineDescription>();

        foreach (var registration in inspector.GetRegistrations())
        {
            if (registration.CandidateHandlerTypes.Count == 0)
            {
                descriptions.Add(new HandlerPipelineDescription(path, registration.QueryType, registration.ResultType, null));
                continue;
            }

            foreach (var candidate in registration.CandidateHandlerTypes)
                descriptions.Add(DescribeCandidate(candidate, path, registration.QueryType, registration.ResultType));
        }

        return descriptions;
    }

    private static HandlerPipelineDescription DescribeCandidate(Type? handlerType, PipelinePath path, Type queryType, Type resultType)
    {
        if (handlerType is null)
            return new HandlerPipelineDescription(path, queryType, resultType, null);

        var lookup = path switch
        {
            PipelinePath.Sync => PipelineAttributeReader.FindExecute(handlerType),
            PipelinePath.Async => PipelineAttributeReader.FindExecuteAsync(handlerType),
            _ => PipelineAttributeReader.FindStreamExecuteAsync(handlerType, queryType)
        };

        // A failed lookup (for example an ambiguous method) is not an absent method; it is a description problem.
        var hasPathMethod = lookup.Failure is not null || lookup.Method is not null;
        return new HandlerPipelineDescription(path, queryType, resultType, handlerType, hasPathMethod, MisplacedAttributes(lookup.Method, path), Problems(lookup, path, queryType, resultType));
    }

    private static IReadOnlyList<DescriptionProblem> Problems(ExecuteMethodLookup lookup, PipelinePath path, Type queryType, Type resultType)
    {
        var subject = path == PipelinePath.Sync ? "Execute" : "ExecuteAsync";
        if (lookup.Failure is not null)
            return [new DescriptionProblem(subject, lookup.Failure.Exception.Message)];

        return lookup.Method is null ? [] : ReadProblems(lookup.Method, path, queryType, resultType, subject);
    }

    private static IReadOnlyList<DescriptionProblem> ReadProblems(MethodInfo method, PipelinePath path, Type queryType, Type resultType, string subject) => path switch
    {
        PipelinePath.Sync => ProblemsOf(PipelineAttributeReader.ReadDecorators<QueryHandlerAttribute>(method, queryType, resultType, attr => attr.Step, attr => attr.GetDecoratorType()), subject),
        PipelinePath.Async => ProblemsOf(PipelineAttributeReader.ReadDecorators<QueryHandlerAttributeAsync>(method, queryType, resultType, attr => attr.Step, attr => attr.GetDecoratorType()), subject),
        _ => ProblemsOf(PipelineAttributeReader.ReadDecorators<StreamQueryHandlerAttribute>(method, queryType, resultType, attr => attr.Step, attr => attr.GetDecoratorType()), subject)
    };

    private static IReadOnlyList<DescriptionProblem> ProblemsOf<TAttribute>(DecoratorEntries<TAttribute> read, string subject)
        where TAttribute : Attribute
    {
        if (read.Failure is not null)
            return [new DescriptionProblem(subject, read.Failure.Exception.Message)];

        return read.Entries
            .Where(entry => entry.Failure is not null)
            .Select(entry => new DescriptionProblem(entry.Attribute.GetType().Name, entry.Failure!.Exception.Message))
            .ToList();
    }

    private static IReadOnlyList<MisplacedAttribute> MisplacedAttributes(MethodInfo? method, PipelinePath path)
    {
        if (method is null) return [];

        var check = PipelineAttributeReader.CheckAttributeKinds(method, ToHandlerPath(path));
        var correctKind = CorrectKind(path);
        return check.AttributeTypes.Select(attributeType => new MisplacedAttribute(attributeType, correctKind)).ToList();
    }

    private static HandlerPath ToHandlerPath(PipelinePath path) => path switch
    {
        PipelinePath.Sync => HandlerPath.Sync,
        PipelinePath.Async => HandlerPath.Async,
        _ => HandlerPath.Stream
    };

    private static Type CorrectKind(PipelinePath path) => path switch
    {
        PipelinePath.Sync => typeof(QueryHandlerAttribute),
        PipelinePath.Async => typeof(QueryHandlerAttributeAsync),
        _ => typeof(StreamQueryHandlerAttribute)
    };
}

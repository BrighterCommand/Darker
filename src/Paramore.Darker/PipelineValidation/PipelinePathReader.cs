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
using System.Linq;
using System.Reflection;

namespace Paramore.Darker.PipelineValidation;

/// <summary>
/// Reads a handler's pipeline as one registry path sees it: which method it runs, which decorator attributes that
/// method should carry, and which handler interface the path requires.
/// </summary>
internal abstract class PipelinePathReader
{
    /// <summary>The reader for the sync registry.</summary>
    public static readonly PipelinePathReader Sync = new SyncPipelinePathReader();

    /// <summary>The reader for the async registry.</summary>
    public static readonly PipelinePathReader Async = new AsyncPipelinePathReader();

    /// <summary>The reader for the stream registry.</summary>
    public static readonly PipelinePathReader Stream = new StreamPipelinePathReader();

    /// <summary>The path this reader reads.</summary>
    public abstract PipelinePath Path { get; }

    /// <summary>The path as the attribute reader names it.</summary>
    public abstract HandlerPath HandlerPath { get; }

    /// <summary>The name of the method the path runs.</summary>
    public abstract string MethodName { get; }

    /// <summary>The base attribute type the path's method should carry.</summary>
    public abstract Type AttributeKind { get; }

    /// <summary>The open generic handler interface the path requires.</summary>
    public abstract Type PathInterface { get; }

    /// <summary>Finds the reader for <paramref name="path"/>, or null when the path is not one Darker defines.</summary>
    public static PipelinePathReader? For(PipelinePath path) => path switch
    {
        PipelinePath.Sync => Sync,
        PipelinePath.Async => Async,
        PipelinePath.Stream => Stream,
        _ => null
    };

    /// <summary>Finds the handler's method for the path.</summary>
    public abstract ExecuteMethodLookup FindMethod(Type handlerType, Type queryType);

    /// <summary>Reads the path's decorator attributes on <paramref name="method"/> and closes them over the registration's types.</summary>
    public abstract PipelineRead ReadDecorators(MethodInfo method, HandlerRegistration registration);

    protected PipelineRead ReadOf<TAttribute>(DecoratorEntries<TAttribute> read, Func<TAttribute, int> stepOf)
        where TAttribute : Attribute
    {
        if (read.Failure is not null)
            return new PipelineRead([new DescriptionProblem(MethodName, read.Failure.Exception.Message)], []);

        var problems = read.Entries
            .Where(entry => entry.Failure is not null)
            .Select(entry => new DescriptionProblem(entry.Attribute.GetType().Name, entry.Failure!.Exception.Message))
            .ToList();
        var steps = read.Entries
            .Where(entry => entry.Failure is null && entry.DecoratorType is not null)
            .Select(entry => new PipelineStepDescription(stepOf(entry.Attribute), entry.Attribute.GetType(), entry.DecoratorType!))
            .ToList();
        return new PipelineRead(problems, steps);
    }
}

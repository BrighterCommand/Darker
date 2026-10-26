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
using System.Reflection;

namespace Paramore.Darker.PipelineValidation;

/// <summary>
/// Reads a handler's pipeline as the stream registry sees it: <c>ExecuteAsync</c> with <see cref="StreamQueryHandlerAttribute"/> decorators.
/// </summary>
internal sealed class StreamPipelinePathReader : PipelinePathReader
{
    public override PipelinePath Path => PipelinePath.Stream;

    public override HandlerPath HandlerPath => HandlerPath.Stream;

    public override string MethodName => PipelineAttributeReader.EXECUTE_ASYNC_METHOD_NAME;

    public override Type AttributeKind => typeof(StreamQueryHandlerAttribute);

    public override Type PathInterface => typeof(IStreamQueryHandler<,>);

    public override Type QueryInterface => typeof(IStreamQuery<>);

    public override ExecuteMethodLookup FindMethod(Type handlerType, Type queryType) => PipelineAttributeReader.FindStreamExecuteAsync(handlerType, queryType);

    public override PipelineRead ReadDecorators(MethodInfo method, HandlerRegistration registration) =>
        ReadOf(
            PipelineAttributeReader.ReadDecorators<StreamQueryHandlerAttribute>(
                method, DecoratorQueryType(registration), registration.ResultType, attribute => attribute.Step, attribute => attribute.GetDecoratorType()),
            attribute => attribute.Step);
}

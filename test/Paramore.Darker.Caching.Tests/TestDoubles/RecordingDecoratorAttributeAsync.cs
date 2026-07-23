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

using System;

namespace Paramore.Darker.Caching.Tests.TestDoubles;

/// <summary>
/// Attribute that wires <see cref="RecordingDecoratorAsync{TQuery,TResult}"/> into the
/// async decorator pipeline. Used in ordering/short-circuit tests to verify that an inner
/// decorator (higher step) is skipped when the outer cache decorator (lower step) returns a hit.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class RecordingDecoratorAttributeAsync : QueryHandlerAttributeAsync
{
    /// <summary>
    /// Initialises the attribute with a pipeline step ordering value.
    /// </summary>
    /// <param name="step">
    /// The step order for this decorator in the pipeline. A higher value than the cache
    /// decorator's step places this decorator <em>inside</em> the cache, so it is skipped on a hit.
    /// </param>
    public RecordingDecoratorAttributeAsync(int step) : base(step)
    {
    }

    /// <inheritdoc />
    public override Type GetDecoratorType() => typeof(RecordingDecoratorAsync<,>);

    /// <inheritdoc />
    public override object[] GetAttributeParams() => [];
}

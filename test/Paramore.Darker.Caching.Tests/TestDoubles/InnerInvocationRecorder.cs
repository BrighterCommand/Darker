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

namespace Paramore.Darker.Caching.Tests.TestDoubles;

/// <summary>
/// Records separate invocation counts for the inner recording decorator and the handler in
/// decorator-ordering / short-circuit tests. Register as a singleton so the same instance
/// is shared between the decorator, the handler, and the test assertion.
/// </summary>
public sealed class InnerInvocationRecorder
{
    /// <summary>Gets the number of times the inner recording decorator was entered.</summary>
    public int InnerDecoratorCallCount { get; private set; }

    /// <summary>Gets the number of times the handler body was entered.</summary>
    public int HandlerCallCount { get; private set; }

    /// <summary>Records one invocation of the inner recording decorator.</summary>
    public void IncrementInnerDecorator() => InnerDecoratorCallCount++;

    /// <summary>Records one invocation of the handler.</summary>
    public void IncrementHandler() => HandlerCallCount++;
}

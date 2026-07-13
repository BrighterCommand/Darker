// Copyright (c) 2025, Ian Cooper
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without modification, are permitted provided that the
// following conditions are met:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the
// Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING
// BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>
    /// Attribute that wires <see cref="StreamStepEventDecorator{TQuery,TResult}"/> into the
    /// stream pipeline for use in step-ordering tests.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    internal sealed class StreamStepEventAttribute : StreamQueryHandlerAttribute
    {
        public StreamStepEventAttribute(int step) : base(step) { }

        public override object[] GetAttributeParams() => new object[] { Step };

        public override Type GetDecoratorType() => typeof(StreamStepEventDecorator<,>);
    }

    /// <summary>
    /// A pass-through stream decorator that records its step number into a shared list when
    /// it first executes, allowing tests to verify that the stream pipeline orders decorators
    /// by <see cref="StreamQueryHandlerAttribute.Step"/> descending.
    /// </summary>
    internal sealed class StreamStepEventDecorator<TQuery, TResult> : IStreamQueryHandlerDecorator<TQuery, TResult>
        where TQuery : IStreamQuery<TResult>
    {
        private readonly List<int> _enteredSteps;
        private int _step;

        public StreamStepEventDecorator(List<int> enteredSteps) => _enteredSteps = enteredSteps;

        public IQueryContext Context { get; set; }

        public void InitializeFromAttributeParams(object[] attributeParams)
        {
            _step = (int)attributeParams[0];
        }

        public async IAsyncEnumerable<TResult> Execute(
            TQuery query,
            Func<TQuery, CancellationToken, IAsyncEnumerable<TResult>> next,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var first = true;
            await foreach (var item in next(query, cancellationToken))
            {
                if (first) { _enteredSteps.Add(_step); first = false; }
                yield return item;
            }
        }
    }
}

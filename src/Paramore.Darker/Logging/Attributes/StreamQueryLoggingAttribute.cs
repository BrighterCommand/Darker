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
using Paramore.Darker.Logging.Handlers;

namespace Paramore.Darker.Logging.Attributes
{
    /// <summary>
    /// Wires <see cref="StreamQueryLoggingDecorator{TQuery,TResult}"/> into the stream pipeline.
    /// Logs stream start (with serialised query body), item count, and elapsed duration on completion.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class StreamQueryLoggingAttribute : StreamQueryHandlerAttribute
    {
        public StreamQueryLoggingAttribute(int step) : base(step) { }

        public override object[] GetAttributeParams() => new object[0];

        public override Type GetDecoratorType() => typeof(StreamQueryLoggingDecorator<,>);
    }
}

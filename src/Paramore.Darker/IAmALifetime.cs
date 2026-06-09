#region Licence
/* The MIT License (MIT)
Copyright © 2024 Ian Cooper <ian_hammond_cooper@yahoo.co.uk>

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

namespace Paramore.Darker
{
    /// <summary>
    /// Owns the lifetime of objects created for a single query pipeline execution.
    /// Disposed by the <see cref="PipelineBuilder{TResult}"/> when the pipeline completes,
    /// including when it completes by throwing or being cancelled.
    /// </summary>
    public interface IAmALifetime : IDisposable
    {
        /// <summary>
        /// Tracks a per-query disposable (for example a child service scope) so that it is
        /// disposed when the pipeline completes.
        /// </summary>
        /// <param name="disposable">The disposable to track for the lifetime of the pipeline execution.</param>
        void Add(IDisposable disposable);
    }
}

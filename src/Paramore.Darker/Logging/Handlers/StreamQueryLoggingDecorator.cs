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
using System.Diagnostics;
#if NET8_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using System.Text.Json;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Paramore.Darker.Logging.Handlers
{
    /// <summary>
    /// A stream decorator that logs stream start (with serialised query body), yields each item
    /// unchanged, and logs completion with item count and elapsed duration.
    /// Exceptions during enumeration are handled by T019's decorator (this decorator does not swallow).
    /// </summary>
    public class StreamQueryLoggingDecorator<TQuery, TResult> : IStreamQueryHandlerDecorator<TQuery, TResult>
        where TQuery : IStreamQuery<TResult>
    {
        private static readonly ILogger Logger = ApplicationLogging.CreateLogger<StreamQueryLoggingDecorator<TQuery, TResult>>();

        public IQueryContext Context { get; set; }

        public void InitializeFromAttributeParams(object[] attributeParams)
        {
            // nothing to do
        }

        public async IAsyncEnumerable<TResult> Execute(
            TQuery query,
            Func<TQuery, CancellationToken, IAsyncEnumerable<TResult>> next,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var sw = Stopwatch.StartNew();
            var queryName = query.GetType().Name;
            var itemCount = 0;

            Logger.LogInformation("Executing stream query {QueryName}: {Query}", queryName, Serialize(query));

            try
            {
                await foreach (var item in next(query, cancellationToken))
                {
                    itemCount++;
                    yield return item;
                }
            }
            finally
            {
                Logger.LogInformation(
                    "Stream execution of query {QueryName} completed; {ItemCount} items in {Elapsed}ms",
                    queryName, itemCount, sw.Elapsed.TotalMilliseconds);
            }
        }

#if NET8_0_OR_GREATER
        [UnconditionalSuppressMessage(
            "Trimming", "IL2026:RequiresUnreferencedCodeAttribute",
            Justification = "Same as QueryLoggingDecoratorAsync — consumers supply their own JsonSerializerOptions.")]
        [UnconditionalSuppressMessage(
            "AOT", "IL3050:RequiresDynamicCodeAttribute",
            Justification = "Same as QueryLoggingDecoratorAsync — source-gen TypeInfoResolver is the supported escape hatch.")]
#endif
        private string Serialize<T>(T value) =>
            JsonSerializer.Serialize(value, value.GetType(), QueryLoggingJsonOptions.Options);
    }
}

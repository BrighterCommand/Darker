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
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Paramore.Darker
{
    /// <summary>
    /// The default <see cref="IAmALifetime"/> implementation. Tracks the disposables created for a
    /// single query pipeline execution (for example a child service scope) and disposes them, in
    /// reverse order of registration, exactly once when the pipeline completes.
    /// </summary>
    internal sealed class QueryLifetimeScope : IAmALifetime
    {
        private readonly ILogger _logger;
        private readonly List<IDisposable> _trackedDisposables = new List<IDisposable>();
        private bool _disposed;

        public QueryLifetimeScope(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public void Add(IDisposable disposable)
        {
            if (disposable == null)
                throw new ArgumentNullException(nameof(disposable));

            _trackedDisposables.Add(disposable);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            for (var i = _trackedDisposables.Count - 1; i >= 0; i--)
                DisposeItem(_trackedDisposables[i]);

            _trackedDisposables.Clear();
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            if (_disposed)
                return;

            _disposed = true;

            for (var i = _trackedDisposables.Count - 1; i >= 0; i--)
                await DisposeItemAsync(_trackedDisposables[i]).ConfigureAwait(false);

            _trackedDisposables.Clear();
        }

        private async ValueTask DisposeItemAsync(IDisposable item)
        {
            try
            {
                if (item is IAsyncDisposable asyncDisposable)
                    await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                else
                    item.Dispose();
            }
            catch (Exception e)
            {
                TryLogTeardownFault(e, item);
            }
        }

        private void DisposeItem(IDisposable item)
        {
            try
            {
                item.Dispose();
            }
            catch (Exception e)
            {
                TryLogTeardownFault(e, item);
            }
        }

        // A faulty logging provider must not break the never-throw teardown guarantee.
        private void TryLogTeardownFault(Exception fault, IDisposable item)
        {
            try
            {
                _logger.LogError(fault, "Error disposing tracked lifetime item of type {ItemType}", item.GetType());
            }
            catch
            {
                // Nothing safe left to do: the fault could not be reported.
            }
        }
    }
}

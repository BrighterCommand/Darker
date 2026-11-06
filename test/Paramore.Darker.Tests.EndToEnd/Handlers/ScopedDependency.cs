// Licensed under the MIT License.
// Copyright (c) .NET Foundation and Contributors.

using System;

namespace Paramore.Darker.Tests.EndToEnd.Handlers
{
    /// <summary>A Scoped disposable dependency that records whether it has been disposed.</summary>
    public sealed class ScopedDependency : IDisposable
    {
        /// <summary>Gets a value indicating whether <see cref="Dispose"/> has been called.</summary>
        public bool IsDisposed { get; private set; }

        /// <inheritdoc />
        public void Dispose()
        {
            IsDisposed = true;
        }
    }
}

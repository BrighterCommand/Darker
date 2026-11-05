// Licensed under the MIT License.
// Copyright (c) .NET Foundation and Contributors.

using System;

namespace Paramore.Darker.Tests.EndToEnd.Handlers
{
    /// <summary>A dedicated transient failure that the retry pipeline is configured to handle.</summary>
    public sealed class TransientFailureException : Exception
    {
        /// <summary>Initializes a new instance of the <see cref="TransientFailureException"/> class.</summary>
        public TransientFailureException()
            : base("Simulated transient failure.")
        {
        }
    }
}

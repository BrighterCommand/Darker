// Licensed under the MIT License.
// Copyright (c) .NET Foundation and Contributors.

namespace Paramore.Darker.Tests.EndToEnd.Handlers
{
    /// <summary>An asynchronous query whose handler fails on its first call and is decorated with a retry pipeline.</summary>
    public sealed class RetriedQuery : IQuery<string>
    {
    }
}

// Licensed under the MIT License.
// Copyright (c) .NET Foundation and Contributors.

namespace Paramore.Darker.Tests.EndToEnd.Handlers
{
    /// <summary>A query whose handler is decorated with a decorator the scenario deliberately leaves unregistered.</summary>
    public sealed class UnregisteredDecoratorQuery : IQuery<string>
    {
    }
}

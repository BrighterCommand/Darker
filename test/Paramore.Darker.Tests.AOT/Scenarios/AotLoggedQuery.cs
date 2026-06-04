// Licensed under the MIT License.
// Copyright (c) .NET Foundation and Contributors.

using System;
using System.Threading;
using System.Threading.Tasks;
using Paramore.Darker;
using Paramore.Darker.Logging.Attributes;

namespace Paramore.Darker.Tests.AOT.Scenarios
{
    /// <summary>
    /// A property-bearing query whose source-generated JSON the harness pins exactly.
    /// The positional record yields <c>Id</c> then <c>Name</c> in declaration order, which is the
    /// order System.Text.Json emits under the source-generated <c>AotTestJsonContext</c> resolver.
    /// </summary>
    internal sealed record AotLoggedQuery(Guid Id, string Name) : IQuery<AotLoggedQuery.Result>
    {
        internal sealed class Result
        {
        }
    }

    internal sealed class AotLoggedQueryHandler : QueryHandlerAsync<AotLoggedQuery, AotLoggedQuery.Result>
    {
        [QueryLoggingAttributeAsync(1)]
        public override Task<AotLoggedQuery.Result> ExecuteAsync(AotLoggedQuery query,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new AotLoggedQuery.Result());
    }
}

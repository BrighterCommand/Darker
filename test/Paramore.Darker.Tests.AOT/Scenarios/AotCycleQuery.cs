// Licensed under the MIT License.
// Copyright (c) .NET Foundation and Contributors.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Paramore.Darker;
using Paramore.Darker.Logging.Attributes;

namespace Paramore.Darker.Tests.AOT.Scenarios
{
    /// <summary>
    /// A query whose object graph contains a reference cycle: a <see cref="AotParent"/> holds its
    /// <see cref="AotChild"/> instances, and each child points back at its parent. Serialising this
    /// graph would throw a <see cref="System.Text.Json.JsonException"/> ("a possible object cycle was
    /// detected") under the System.Text.Json defaults — it does not throw only because
    /// <c>QueryLoggingJsonOptions.Options</c> carries <c>ReferenceHandler.IgnoreCycles</c> (FR3). This
    /// scenario proves that default holds under native AOT, exactly as it does under the JIT.
    /// </summary>
    internal sealed class AotCycleQuery : IQuery<AotCycleQuery.Result>
    {
        public AotCycleQuery(AotParent root) => Root = root;

        public AotParent Root { get; }

        internal sealed class Result
        {
        }
    }

    internal sealed class AotParent
    {
        public string Name { get; set; } = string.Empty;

        public List<AotChild> Children { get; } = new();
    }

    internal sealed class AotChild
    {
        public string Name { get; set; } = string.Empty;

        public AotParent? Parent { get; set; }
    }

    internal sealed class AotCycleQueryHandler : QueryHandlerAsync<AotCycleQuery, AotCycleQuery.Result>
    {
        [QueryLoggingAttributeAsync(1)]
        public override Task<AotCycleQuery.Result> ExecuteAsync(AotCycleQuery query,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new AotCycleQuery.Result());
    }
}

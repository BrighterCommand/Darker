// Licensed under the MIT License.
// Copyright (c) .NET Foundation and Contributors.

using System;
using System.Threading;
using System.Threading.Tasks;
using Paramore.Darker;
using Paramore.Darker.Policies.Attributes;

namespace Paramore.Darker.Tests.AOT.Scenarios
{
    /// <summary>A query whose handlers always fall back; the result echoes the query's id so the fallback is evident.</summary>
    public sealed class AotFallbackQuery : IQuery<AotFallbackQuery.Result>
    {
        public AotFallbackQuery(Guid id) => Id = id;

        public Guid Id { get; }

        public sealed class Result
        {
            public Guid Value { get; init; }
        }
    }

    /// <summary>Sync handler: Execute throws under [FallbackPolicy]; the overridden Fallback returns the query's id.</summary>
    public sealed class AotFallbackQueryHandler : QueryHandler<AotFallbackQuery, AotFallbackQuery.Result>
    {
        [FallbackPolicy(1)]
        public override AotFallbackQuery.Result Execute(AotFallbackQuery query)
            => throw new InvalidOperationException("trigger fallback");

        public override AotFallbackQuery.Result Fallback(AotFallbackQuery query)
            => new AotFallbackQuery.Result { Value = query.Id };
    }

    /// <summary>Async handler: ExecuteAsync throws under [FallbackPolicyAttributeAsync]; the overridden FallbackAsync returns the query's id.</summary>
    public sealed class AotFallbackQueryHandlerAsync : QueryHandlerAsync<AotFallbackQuery, AotFallbackQuery.Result>
    {
        [FallbackPolicyAttributeAsync(1)]
        public override Task<AotFallbackQuery.Result> ExecuteAsync(AotFallbackQuery query, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("trigger fallback");

        public override Task<AotFallbackQuery.Result> FallbackAsync(AotFallbackQuery query, CancellationToken cancellationToken = default)
            => Task.FromResult(new AotFallbackQuery.Result { Value = query.Id });
    }

    public sealed class DirectFallbackQueryHandler : IQueryHandler<AotFallbackQuery, AotFallbackQuery.Result>
    {
        public IQueryContext Context { get; set; } = null!;

        [FallbackPolicy(1)]
        public AotFallbackQuery.Result Execute(AotFallbackQuery query)
            => throw new InvalidOperationException("trigger fallback");

        AotFallbackQuery.Result IQueryHandler<AotFallbackQuery, AotFallbackQuery.Result>.Fallback(AotFallbackQuery query)
            => new AotFallbackQuery.Result { Value = query.Id };
    }

    public sealed class DirectFallbackQueryHandlerAsync : IQueryHandlerAsync<AotFallbackQuery, AotFallbackQuery.Result>
    {
        public IQueryContext Context { get; set; } = null!;

        [FallbackPolicyAttributeAsync(1)]
        public Task<AotFallbackQuery.Result> ExecuteAsync(AotFallbackQuery query, CancellationToken cancellationToken)
            => throw new InvalidOperationException("trigger fallback");

        Task<AotFallbackQuery.Result> IQueryHandlerAsync<AotFallbackQuery, AotFallbackQuery.Result>.FallbackAsync(
            AotFallbackQuery query, CancellationToken cancellationToken)
            => Task.FromResult(new AotFallbackQuery.Result { Value = query.Id });
    }
}

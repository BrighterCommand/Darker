using Paramore.Darker.Observability;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class When_reading_cache_semantic_conventions_should_expose_cache_names_and_tags
    {
        [Fact]
        public void When_reading_cache_semantic_conventions_should_expose_cache_names_and_tags_fact()
        {
            // Arrange — nothing to arrange; constants and sets are static and stateless

            // Act — read all public cache-related members from the conventions class
            var cacheOutcome = DarkerSemanticConventions.CacheOutcome;
            var cacheRequestsMetricName = DarkerSemanticConventions.CacheRequestsMetricName;
            var cacheRequestsAllowedTags = DarkerSemanticConventions.CacheRequestsAllowedTags;

            // Assert — string constants match documented values; allowed-tag set has correct membership
            cacheOutcome.ShouldBe("paramore.darker.cache.outcome");
            cacheRequestsMetricName.ShouldBe("paramore.darker.cache.requests");

            // CacheRequestsAllowedTags: exactly QueryType and CacheOutcome (no high-cardinality keys)
            cacheRequestsAllowedTags.Count.ShouldBe(2);
            cacheRequestsAllowedTags.ShouldContain(DarkerSemanticConventions.QueryType);
            cacheRequestsAllowedTags.ShouldContain(DarkerSemanticConventions.CacheOutcome);
            cacheRequestsAllowedTags.ShouldNotContain(DarkerSemanticConventions.QueryId);
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Paramore.Darker.Extensions.Diagnostics.Observability;
using Paramore.Darker.Observability;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Diagnostics.Tests;

[Collection("DarkerMeter")]
public class MetricTagEnrichmentTests
{
    [Fact]
    public void When_enriching_metric_tags_should_filter_to_allowed_keys_and_read_service_attributes()
    {
        // ── Filter: drops high-cardinality tags ───────────────────────────────

        //Arrange
        var tags = new List<KeyValuePair<string, object?>>
        {
            new(DarkerSemanticConventions.QueryType, "MyQuery"),
            new(DarkerSemanticConventions.QueryId, "some-id"),
            new(DarkerSemanticConventions.QueryBody, "{\"foo\":1}"),
            new(DarkerSemanticConventions.ErrorType, "System.Exception"),
        };

        //Act
        var filtered = tags.Filter(DarkerSemanticConventions.QueryDurationAllowedTags);

        //Assert
        filtered.Length.ShouldBe(2);
        filtered.ShouldContain(p => p.Key == DarkerSemanticConventions.QueryType);
        filtered.ShouldContain(p => p.Key == DarkerSemanticConventions.ErrorType);
        filtered.ShouldNotContain(p => p.Key == DarkerSemanticConventions.QueryId);
        filtered.ShouldNotContain(p => p.Key == DarkerSemanticConventions.QueryBody);

        // ── GetServiceAttributes: reads service.name from MeterProvider resource ──

        //Arrange
        using var meterProvider = Sdk.CreateMeterProviderBuilder()
            .ConfigureResource(r => r.AddService("svc-a"))
            .Build()!;

        //Act
        var serviceAttributes = meterProvider.GetServiceAttributes();

        //Assert
        var serviceNamePair = serviceAttributes
            .Where(a => a.Key == DarkerSemanticConventions.ServiceName)
            .ShouldHaveSingleItem();
        serviceNamePair.Value.ShouldBe("svc-a");
    }
}

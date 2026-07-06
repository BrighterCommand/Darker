using Paramore.Darker.Observability;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class When_reading_metric_semantic_conventions_should_expose_meter_and_metric_names
    {
        [Fact]
        public void Should_expose_meter_and_metric_names()
        {
            // Arrange — nothing to arrange; constants and sets are static and stateless

            // Act — read all public metric-related members from the conventions class
            var meterName = DarkerSemanticConventions.MeterName;
            var queryDurationMetricName = DarkerSemanticConventions.QueryDurationMetricName;
            var dbClientOperationDurationMetricName = DarkerSemanticConventions.DbClientOperationDurationMetricName;
            var serviceName = DarkerSemanticConventions.ServiceName;
            var serviceVersion = DarkerSemanticConventions.ServiceVersion;
            var serviceInstanceId = DarkerSemanticConventions.ServiceInstanceId;
            var serviceNamespace = DarkerSemanticConventions.ServiceNamespace;
            var queryDurationAllowedTags = DarkerSemanticConventions.QueryDurationAllowedTags;
            var dbClientOperationDurationAllowedTags = DarkerSemanticConventions.DbClientOperationDurationAllowedTags;

            // Assert — string constants match documented values; allowed-tag sets have correct membership
            meterName.ShouldBe("paramore.darker");
            queryDurationMetricName.ShouldBe("paramore.darker.query.duration");
            dbClientOperationDurationMetricName.ShouldBe("db.client.operation.duration");

            serviceName.ShouldBe("service.name");
            serviceVersion.ShouldBe("service.version");
            serviceInstanceId.ShouldBe("service.instance.id");
            serviceNamespace.ShouldBe("service.namespace");

            // QueryDurationAllowedTags: exactly QueryType, Operation, ErrorType (no high-cardinality keys)
            queryDurationAllowedTags.Count.ShouldBe(3);
            queryDurationAllowedTags.ShouldContain(DarkerSemanticConventions.QueryType);
            queryDurationAllowedTags.ShouldContain(DarkerSemanticConventions.Operation);
            queryDurationAllowedTags.ShouldContain(DarkerSemanticConventions.ErrorType);
            queryDurationAllowedTags.ShouldNotContain(DarkerSemanticConventions.QueryId);
            queryDurationAllowedTags.ShouldNotContain(DarkerSemanticConventions.QueryBody);

            // DbClientOperationDurationAllowedTags: exactly DbSystem, DbName, DbOperation, DbSqlTable, DbCollectionName, ServerAddress, ErrorType
            dbClientOperationDurationAllowedTags.Count.ShouldBe(7);
            dbClientOperationDurationAllowedTags.ShouldContain(DarkerSemanticConventions.DbSystem);
            dbClientOperationDurationAllowedTags.ShouldContain(DarkerSemanticConventions.DbName);
            dbClientOperationDurationAllowedTags.ShouldContain(DarkerSemanticConventions.DbOperation);
            dbClientOperationDurationAllowedTags.ShouldContain(DarkerSemanticConventions.DbSqlTable);
            dbClientOperationDurationAllowedTags.ShouldContain(DarkerSemanticConventions.DbCollectionName);
            dbClientOperationDurationAllowedTags.ShouldContain(DarkerSemanticConventions.ServerAddress);
            dbClientOperationDurationAllowedTags.ShouldContain(DarkerSemanticConventions.ErrorType);
            dbClientOperationDurationAllowedTags.ShouldNotContain(DarkerSemanticConventions.DbStatement);
            dbClientOperationDurationAllowedTags.ShouldNotContain(DarkerSemanticConventions.DbUser);
        }
    }
}

using System.Diagnostics;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.Observability;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    [Collection("DarkerActivitySource")]
    public class When_creating_db_span_should_nest_under_parent_with_db_attributes
    {
        [Fact]
        public void Should_nest_db_span_under_parent_query_span_with_client_kind()
        {
            // Arrange
            using var listener = new ActivityListener
            {
                ShouldListenTo = s => s.Name == "paramore.darker",
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData,
            };
            ActivitySource.AddActivityListener(listener);
            using var tracer = new DarkerTracer();
            var query = new SomeQuery();
            var parentSpan = tracer.CreateQuerySpan(query, options: InstrumentationOptions.QueryInformation);
            var info = new DbSpanInfo(DbSystem.PostgreSql, "orders", "select", "order");

            // Act
            var dbSpan = tracer.CreateDbSpan(info, parentSpan, InstrumentationOptions.DatabaseInformation);

            // Assert
            try
            {
                dbSpan.ShouldNotBeNull();
                dbSpan.Kind.ShouldBe(ActivityKind.Client);
                dbSpan.ParentId.ShouldBe(parentSpan!.Id);
            }
            finally
            {
                dbSpan?.Stop();
                parentSpan?.Stop();
            }
        }

        [Fact]
        public void Should_set_display_name_with_table_when_table_is_provided()
        {
            // Arrange
            using var listener = new ActivityListener
            {
                ShouldListenTo = s => s.Name == "paramore.darker",
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData,
            };
            ActivitySource.AddActivityListener(listener);
            using var tracer = new DarkerTracer();
            var info = new DbSpanInfo(DbSystem.PostgreSql, "orders", "select", "order");

            // Act
            var dbSpan = tracer.CreateDbSpan(info, null, InstrumentationOptions.DatabaseInformation);

            // Assert
            try
            {
                dbSpan.ShouldNotBeNull();
                dbSpan.DisplayName.ShouldBe("select orders order");
            }
            finally
            {
                dbSpan?.Stop();
            }
        }

        [Fact]
        public void Should_set_display_name_without_table_when_table_is_null()
        {
            // Arrange
            using var listener = new ActivityListener
            {
                ShouldListenTo = s => s.Name == "paramore.darker",
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData,
            };
            ActivitySource.AddActivityListener(listener);
            using var tracer = new DarkerTracer();
            var info = new DbSpanInfo(DbSystem.PostgreSql, "orders", "select");

            // Act
            var dbSpan = tracer.CreateDbSpan(info, null, InstrumentationOptions.DatabaseInformation);

            // Assert
            try
            {
                dbSpan.ShouldNotBeNull();
                dbSpan.DisplayName.ShouldBe("select orders");
            }
            finally
            {
                dbSpan?.Stop();
            }
        }

        [Fact]
        public void Should_tag_db_attributes_when_database_information_option_is_set()
        {
            // Arrange
            using var listener = new ActivityListener
            {
                ShouldListenTo = s => s.Name == "paramore.darker",
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData,
            };
            ActivitySource.AddActivityListener(listener);
            using var tracer = new DarkerTracer();
            var info = new DbSpanInfo(DbSystem.PostgreSql, "orders", "select", "order");

            // Act
            var dbSpan = tracer.CreateDbSpan(info, null, InstrumentationOptions.DatabaseInformation);

            // Assert
            try
            {
                dbSpan.ShouldNotBeNull();
                dbSpan.IsAllDataRequested.ShouldBeTrue();
                dbSpan.GetTagItem(DarkerSemanticConventions.DbSystem).ShouldBe("postgresql");
                dbSpan.GetTagItem(DarkerSemanticConventions.DbName).ShouldBe("orders");
                dbSpan.GetTagItem(DarkerSemanticConventions.DbOperation).ShouldBe("select");
            }
            finally
            {
                dbSpan?.Stop();
            }
        }

        [Fact]
        public void Should_return_null_when_no_listener_is_registered()
        {
            // Arrange — no ActivityListener registered; zero-overhead no-listener path
            using var tracer = new DarkerTracer();
            var info = new DbSpanInfo(DbSystem.PostgreSql, "orders", "select", "order");

            // Act
            var dbSpan = tracer.CreateDbSpan(info, null, InstrumentationOptions.DatabaseInformation);

            // Assert
            dbSpan.ShouldBeNull();
        }
    }
}

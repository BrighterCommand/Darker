using Paramore.Darker.Observability;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class When_combining_instrumentation_options_should_expose_flag_groups
    {
        [Fact]
        public void Should_expose_flag_groups()
        {
            // Arrange — nothing to arrange; enum values are constants

            // Act — read individual flag values and the combined All value
            var none = InstrumentationOptions.None;
            var queryInformation = InstrumentationOptions.QueryInformation;
            var queryBody = InstrumentationOptions.QueryBody;
            var queryContext = InstrumentationOptions.QueryContext;
            var databaseInformation = InstrumentationOptions.DatabaseInformation;
            var all = InstrumentationOptions.All;

            // Assert — each flag has the expected bit value and All is the union
            ((int)none).ShouldBe(0);
            ((int)queryInformation).ShouldBe(1);
            ((int)queryBody).ShouldBe(2);
            ((int)queryContext).ShouldBe(4);
            ((int)databaseInformation).ShouldBe(8);

            all.ShouldBe(InstrumentationOptions.QueryInformation | InstrumentationOptions.QueryBody | InstrumentationOptions.QueryContext | InstrumentationOptions.DatabaseInformation);
            all.HasFlag(InstrumentationOptions.QueryBody).ShouldBeTrue();

            none.HasFlag(InstrumentationOptions.QueryInformation).ShouldBeFalse();
        }
    }
}

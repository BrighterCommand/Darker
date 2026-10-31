using System;
using Paramore.Darker.Core.Tests.Exported;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.PipelineValidation;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests.PipelineValidation
{
    public class CleanConfigurationValidationTests
    {
        [Fact]
        public void When_validating_a_clean_configuration_should_report_no_errors()
        {
            // Arrange — the registries hold only correctly configured public handlers
            var syncRegistry = new QueryHandlerRegistry();
            syncRegistry.Register<TestQueryA, Guid, TestQueryHandler>();
            var asyncRegistry = new QueryHandlerRegistryAsync();
            asyncRegistry.Register<TestQueryA, Guid, TestQueryHandlerAsync>();
            var streamRegistry = new StreamQueryHandlerRegistry();
            streamRegistry.Register<ExportedStreamQuery, string, ExportedStreamQueryHandler>();

            var validator = new PipelineValidatorBuilder()
                .WithSyncRegistry(syncRegistry)
                .WithAsyncRegistry(asyncRegistry)
                .WithStreamRegistry(streamRegistry)
                .Build();

            // Act
            PipelineValidationResult? result = null;
            Should.NotThrow(() => result = validator.Validate());

            // Assert
            result!.IsValid.ShouldBeTrue();
            result.Errors.ShouldBeEmpty();
        }

        [Fact]
        public void When_no_registries_are_supplied_should_validate_them_as_empty()
        {
            // Arrange — the builder's default is null registries
            var validator = new PipelineValidatorBuilder().Build();

            // Act
            var result = validator.Validate();

            // Assert
            result.IsValid.ShouldBeTrue();
            result.Errors.ShouldBeEmpty();
        }
    }
}

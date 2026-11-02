using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Paramore.Darker.PipelineValidation;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests.PipelineValidation
{
    public class NoCaptiveDependencyTests
    {
        private static readonly ValidationRuleId CaptiveDependency = new ValidationRuleId("CaptiveDependency");
        private static readonly ValidationRuleId RuleEvaluationFailed = new ValidationRuleId("RuleEvaluationFailed");

        [Fact]
        public void When_a_singleton_handler_takes_a_transient_dependency_should_report_nothing()
        {
            // Arrange — a Singleton HandlerLifetime and a handler taking a Transient dependency
            var services = new ServiceCollection();
            services.AddTransient<TransientDependency>();
            services.AddDarker(options => options.HandlerLifetime = ServiceLifetime.Singleton)
                .AddHandlers(r => r.Register<CaptiveQuery, CaptiveResult, TransientDependencyHandler>())
                .ValidatePipelines();
            using var provider = services.BuildServiceProvider();

            // Act
            var result = provider.ValidateAll();

            // Assert — no CaptiveDependency finding
            AllFindings(result).ShouldNotContain(f => f.RuleId == CaptiveDependency);
        }

        [Fact]
        public void When_a_singleton_handler_takes_a_singleton_dependency_should_report_nothing()
        {
            // Arrange — a Singleton HandlerLifetime and a handler taking a Singleton dependency
            var services = new ServiceCollection();
            services.AddSingleton<SingletonDependency>();
            services.AddDarker(options => options.HandlerLifetime = ServiceLifetime.Singleton)
                .AddHandlers(r => r.Register<CaptiveQuery, CaptiveResult, SingletonDependencyHandler>())
                .ValidatePipelines();
            using var provider = services.BuildServiceProvider();

            // Act
            var result = provider.ValidateAll();

            // Assert — no CaptiveDependency finding
            AllFindings(result).ShouldNotContain(f => f.RuleId == CaptiveDependency);
        }

        [Fact]
        public void When_a_singleton_handler_takes_an_IServiceProvider_should_report_nothing()
        {
            // Arrange — a Singleton HandlerLifetime and a handler taking IServiceProvider (no descriptor)
            var services = new ServiceCollection();
            services.AddDarker(options => options.HandlerLifetime = ServiceLifetime.Singleton)
                .AddHandlers(r => r.Register<CaptiveQuery, CaptiveResult, ServiceProviderHandler>())
                .ValidatePipelines();
            using var provider = services.BuildServiceProvider();

            // Act
            var result = provider.ValidateAll();

            // Assert — no CaptiveDependency, no RuleEvaluationFailed, no errors
            AllFindings(result).ShouldNotContain(f => f.RuleId == CaptiveDependency);
            AllFindings(result).ShouldNotContain(f => f.RuleId == RuleEvaluationFailed);
            result.Errors.ShouldBeEmpty();
        }

        [Fact]
        public void When_a_singleton_handler_takes_an_unregistered_dependency_should_report_nothing()
        {
            // Arrange — a Singleton HandlerLifetime and a handler taking a type with no descriptor
            var services = new ServiceCollection();
            services.AddDarker(options => options.HandlerLifetime = ServiceLifetime.Singleton)
                .AddHandlers(r => r.Register<CaptiveQuery, CaptiveResult, UnregisteredDependencyHandler>())
                .ValidatePipelines();
            using var provider = services.BuildServiceProvider();

            // Act
            var result = provider.ValidateAll();

            // Assert — no CaptiveDependency, no RuleEvaluationFailed, no errors
            AllFindings(result).ShouldNotContain(f => f.RuleId == CaptiveDependency);
            AllFindings(result).ShouldNotContain(f => f.RuleId == RuleEvaluationFailed);
            result.Errors.ShouldBeEmpty();
        }

        [Fact]
        public void When_a_singleton_open_generic_decorator_takes_an_IServiceProvider_should_report_nothing()
        {
            // Arrange — a Singleton HandlerLifetime and an open-generic decorator taking IServiceProvider
            var services = new ServiceCollection();
            var builder = services.AddDarker(options => options.HandlerLifetime = ServiceLifetime.Singleton);
            builder.RegisterDecorator(typeof(ServiceProviderDecorator<,>));
            builder.ValidatePipelines();
            using var provider = services.BuildServiceProvider();

            // Act
            var result = provider.ValidateAll();

            // Assert — no CaptiveDependency, no RuleEvaluationFailed, no errors
            AllFindings(result).ShouldNotContain(f => f.RuleId == CaptiveDependency);
            AllFindings(result).ShouldNotContain(f => f.RuleId == RuleEvaluationFailed);
            result.Errors.ShouldBeEmpty();
        }

        private static ValidationError[] AllFindings(PipelineValidationResult result) =>
            result.Errors.Concat(result.Warnings).ToArray();
    }
}

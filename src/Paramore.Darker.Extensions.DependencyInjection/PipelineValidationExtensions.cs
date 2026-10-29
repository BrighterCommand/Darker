#region Licence
/* The MIT License (MIT)
Copyright © 2024 Ian Cooper <ian_hammond_cooper@yahoo.co.uk>

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE. */
#endregion

#nullable enable

using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.PipelineValidation;

namespace Paramore.Darker.Extensions.DependencyInjection
{
    /// <summary>
    /// Opt-in registration of startup pipeline validation.
    /// </summary>
    public static class PipelineValidationExtensions
    {
        /// <summary>
        /// Registers the pipeline validator.
        /// </summary>
        /// <param name="builder">The Darker builder returned by AddDarker.</param>
        /// <param name="throwOnError">Whether errors should stop the host (true by default).</param>
        /// <returns>The same builder.</returns>
        /// <exception cref="InvalidOperationException">The builder was not created by AddDarker, so it does not expose the registries.</exception>
        public static IDarkerHandlerBuilder ValidatePipelines(this IDarkerHandlerBuilder builder, bool throwOnError = true)
        {
            var darkerBuilder = builder as ServiceCollectionDarkerHandlerBuilder
                ?? throw new InvalidOperationException(
                    "ValidatePipelines requires the builder returned by AddDarker, because only that builder exposes the handler registries.");

            var options = FindRegistration(builder.Services);
            if (options is null)
            {
                options = new DarkerPipelineValidationOptions();
                builder.Services.AddSingleton(options);
                RegisterValidation(darkerBuilder);
            }

            // The last call sets throwOnError.
            options.ThrowOnError = throwOnError;

            return builder;
        }

        private static DarkerPipelineValidationOptions? FindRegistration(IServiceCollection services) =>
            services
                .Where(d => !d.IsKeyedService && d.ServiceType == typeof(DarkerPipelineValidationOptions))
                .Select(d => d.ImplementationInstance as DarkerPipelineValidationOptions)
                .FirstOrDefault(o => o is not null);

        private static void RegisterValidation(ServiceCollectionDarkerHandlerBuilder darkerBuilder)
        {
            var services = darkerBuilder.Services;
            var sync = darkerBuilder.SyncRegistry;
            var async = darkerBuilder.AsyncRegistry;
            var stream = darkerBuilder.StreamRegistry;

            // AddSingleton, not TryAddEnumerable: the latter drops equal-valued records, and packages add their own rules beside these.
            foreach (var rule in DecoratorOrderingRules.Default)
                services.AddSingleton(rule);

            // Built lazily, at resolve time, over the live registries, so registration order does not matter.
            services.AddSingleton<IAmAPipelineValidator>(provider =>
                new PipelineValidator(sync, async, stream, provider.GetServices<DecoratorOrderingRule>(), ServiceProviderResolvabilityProbe.From(provider)));

            services.AddHostedService<DarkerValidationHostedService>();
        }

        /// <summary>
        /// Runs every registered <see cref="IAmAPipelineValidator"/> and combines their findings in registration order.
        /// </summary>
        /// <param name="provider">The service provider to resolve the validators from.</param>
        /// <returns>One combined result. Findings never cause this method to throw.</returns>
        public static PipelineValidationResult ValidateAll(this IServiceProvider provider) =>
            PipelineValidationResult.Combine(provider.GetServices<IAmAPipelineValidator>().Select(v => v.Validate()));
    }
}

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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Paramore.Darker.PipelineValidation;

namespace Paramore.Darker.Extensions.DependencyInjection
{
    /// <summary>
    /// Runs pipeline validation in the host's starting phase, before any hosted service's StartAsync.
    /// </summary>
    internal sealed class DarkerValidationHostedService : IHostedLifecycleService
    {
        private const string FINDING_TEMPLATE = "Pipeline validation [{RuleId}] {Source}: {Message}";

        private const string CLEAN_MESSAGE = "Pipeline validation found no problems";

        private readonly IServiceProvider _provider;

        private readonly DarkerPipelineValidationOptions _options;

        private readonly ILogger<DarkerValidationHostedService> _logger;

        private int _validated;

        public DarkerValidationHostedService(IServiceProvider provider, DarkerPipelineValidationOptions options, ILogger<DarkerValidationHostedService> logger)
        {
            _logger = logger;
            _provider = provider;
            _options = options;
        }

        public Task StartingAsync(CancellationToken cancellationToken)
        {
            ValidateOnce();
            return Task.CompletedTask;
        }

        private void ValidateOnce()
        {
            if (Interlocked.Exchange(ref _validated, 1) == 0)
                Validate();
        }

        private void Validate()
        {
            var result = _provider.ValidateAll();
            LogWarningsOrSummary(result);
            if (_options.ThrowOnError)
                result.ThrowIfInvalid();
            else
                LogErrors(result);
        }

        private void LogWarningsOrSummary(PipelineValidationResult result)
        {
            foreach (var warning in result.Warnings)
                _logger.LogWarning(FINDING_TEMPLATE, warning.RuleId, warning.Source, warning.Message);
            if (result.Errors.Count == 0 && result.Warnings.Count == 0)
                _logger.LogDebug(CLEAN_MESSAGE);
        }

        private void LogErrors(PipelineValidationResult result)
        {
            foreach (var error in result.Errors)
                _logger.LogError(FINDING_TEMPLATE, error.RuleId, error.Source, error.Message);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            ValidateOnce();
            return Task.CompletedTask;
        }

        public Task StartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}

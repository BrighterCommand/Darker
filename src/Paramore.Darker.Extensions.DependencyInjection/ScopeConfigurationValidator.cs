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
using System.Collections.Generic;
using System.Linq;
using Paramore.Darker.PipelineValidation;
using Paramore.Darker.Specifications;

namespace Paramore.Darker.Extensions.DependencyInjection
{
    /// <summary>
    /// Validates the lifetime and scope configuration of the host.
    /// </summary>
    internal sealed class ScopeConfigurationValidator : IAmAPipelineValidator
    {
        private readonly ServiceRegistrationSnapshot _snapshot;

        public ScopeConfigurationValidator(ServiceRegistrationSnapshot snapshot)
        {
            _snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        }

        public PipelineValidationResult Validate()
        {
            var findings = Evaluate(ScopeConfigurationRules.Create(), new[] { _snapshot.ReadScopeConfiguration() })
                .Concat(Evaluate(new[] { ComponentRules.Captive(_snapshot) }, _snapshot.Components))
                .Where(result => !result.Success && result.Error is not null)
                .Select(result => result.Error!)
                .ToList();

            return new PipelineValidationResult(
                findings.Where(finding => finding.Severity == ValidationSeverity.Error),
                findings.Where(finding => finding.Severity != ValidationSeverity.Error));
        }

        /// <summary>
        /// Runs each rule over each entity, in that order, and collects the results of those the rule is not satisfied by.
        /// A rule holds its last results, so they are collected straight after the rule is evaluated.
        /// </summary>
        private static List<ValidationResult> Evaluate<T>(IEnumerable<ISpecification<T>> rules, IEnumerable<T> entities)
        {
            var collector = new ValidationResultCollector<T>();

            return rules
                .SelectMany(rule => entities
                    .Where(entity => !rule.IsSatisfiedBy(entity))
                    .SelectMany(entity => rule.Accept(collector)))
                .ToList();
        }
    }
}

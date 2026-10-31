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

using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Specifications;

namespace Paramore.Darker.Extensions.DependencyInjection
{
    /// <summary>
    /// Supplies the rules that judge a <see cref="ScopeConfiguration"/>.
    /// </summary>
    /// <remarks>
    /// A <see cref="Specification{T}"/> keeps the results of its last evaluation, so each call to
    /// <see cref="Create"/> returns fresh specifications. Every error factory is null-safe and does not throw.
    /// </remarks>
    internal static class ScopeConfigurationRules
    {
        public static readonly ValidationRuleId InertScopeAffinity = new ValidationRuleId("InertScopeAffinity");
        public static readonly ValidationRuleId DuplicateScopeProvider = new ValidationRuleId("DuplicateScopeProvider");

        private const string ReadmeLinkBase = "https://github.com/BrighterCommand/Darker/blob/master/README.md#";

        public static IReadOnlyList<ISpecification<ScopeConfiguration>> Create() =>
            new List<ISpecification<ScopeConfiguration>> { InertAffinity(), DuplicateProvider() };

        private static string ReadmeLink(ValidationRuleId ruleId) => ReadmeLinkBase + ruleId.ToString().ToLowerInvariant();

        private static Specification<ScopeConfiguration> InertAffinity() =>
            new Specification<ScopeConfiguration>(
                configuration => configuration is null
                    || configuration.Affinity != ScopeAffinity.JoinAmbient
                    || (configuration.HandlerLifetime == ServiceLifetime.Scoped && configuration.ProviderRegistered),
                configuration => new ValidationError(
                    ValidationSeverity.Error,
                    "Scope affinity",
                    InertAffinityMessage(configuration))
                {
                    RuleId = InertScopeAffinity
                });

        private static Specification<ScopeConfiguration> DuplicateProvider() =>
            new Specification<ScopeConfiguration>(
                configuration => configuration is null || configuration.ProviderDescriptions.Distinct().Count() <= 1,
                configuration => new ValidationError(
                    ValidationSeverity.Warning,
                    "Scope provider registration",
                    DuplicateProviderMessage(configuration))
                {
                    RuleId = DuplicateScopeProvider
                });

        private static string DuplicateProviderMessage(ScopeConfiguration? configuration)
        {
            var providers = configuration?.ProviderDescriptions ?? new List<string>();
            var used = providers.Count > 0 ? providers[providers.Count - 1] : "none";
            return $"More than one IAmAScopeProvider is registered: {string.Join(", ", providers)}. {used} is used, because the last registration wins. "
                + "Keep one provider, registered with AddDarkerScopeProvider or AddDarkerRequestScope. "
                + ReadmeLink(DuplicateScopeProvider);
        }

        private static string InertAffinityMessage(ScopeConfiguration? configuration)
        {
            var causes = new List<string>();
            if (configuration?.HandlerLifetime != ServiceLifetime.Scoped)
                causes.Add($"HandlerLifetime is {configuration?.HandlerLifetime}. Set ServiceLifetime.Scoped.");
            if (configuration?.ProviderRegistered != true)
                causes.Add("no IAmAScopeProvider is registered. Call AddDarkerScopeProvider, or AddDarkerRequestScope() in ASP.NET Core.");

            return $"The scope affinity JoinAmbient from {configuration?.AffinitySource} cannot take effect, so each query gets a scope of its own. "
                + string.Join(" ", causes)
                + " Use ScopeAffinity.AlwaysNew if joining is not wanted. "
                + ReadmeLink(InertScopeAffinity);
        }
    }
}

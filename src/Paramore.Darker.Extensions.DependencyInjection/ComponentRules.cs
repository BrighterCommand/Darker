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
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Specifications;

namespace Paramore.Darker.Extensions.DependencyInjection
{
    /// <summary>
    /// The rules that examine one handler or decorator registration at a time.
    /// </summary>
    internal static class ComponentRules
    {
        public static readonly ValidationRuleId CaptiveDependency = new ValidationRuleId("CaptiveDependency");

        public static Specification<ComponentRegistration> Captive(ServiceRegistrationSnapshot snapshot) =>
            new Specification<ComponentRegistration>(component => CaptiveResults(snapshot, component));

        private static IEnumerable<ValidationResult> CaptiveResults(ServiceRegistrationSnapshot snapshot, ComponentRegistration component)
        {
            if (component.SingletonReason is null) return Enumerable.Empty<ValidationResult>();

            var constructor = ComponentConstructorSelector.Select(component.ImplementationType);
            if (constructor is null) return Enumerable.Empty<ValidationResult>();

            return constructor.GetParameters()
                .Where(parameter => snapshot.LastUnkeyedDescriptorOf(parameter.ParameterType)?.Lifetime == ServiceLifetime.Scoped)
                .Select(parameter => ValidationResult.Fail(new ValidationError(
                    ValidationSeverity.Warning,
                    component.ToString(),
                    CaptiveMessage(component, parameter))
                {
                    RuleId = CaptiveDependency
                }));
        }

        private static string CaptiveMessage(ComponentRegistration component, System.Reflection.ParameterInfo parameter) =>
            $"{component.ImplementationType.Name} counts as Singleton because {component.SingletonReason}, but its constructor parameter "
            + $"'{parameter.Name}' of type {parameter.ParameterType.Name} is registered Scoped, so one instance is captured for the life of the container. "
            + $"Register {parameter.ParameterType.Name} Singleton or Transient, or change the lifetime of {component.ImplementationType.Name}. "
            + ScopeConfigurationRules.ReadmeLink(CaptiveDependency);
    }
}

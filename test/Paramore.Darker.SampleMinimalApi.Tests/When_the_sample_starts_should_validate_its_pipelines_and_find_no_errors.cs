#region Licence
/* The MIT License (MIT)
Copyright © 2026 Ian Cooper <ian_hammond_cooper@yahoo.co.uk>

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
using System;
using System.Linq;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.PipelineValidation;
using Shouldly;
using Xunit;

namespace Paramore.Darker.SampleMinimalApi.Tests;

[Collection("SampleMinimalApi")]
public class SampleStartupTests(WebApplicationFactory<Program> factory)
{
    [Fact]
    public void When_the_sample_starts_should_validate_its_pipelines_and_find_no_errors()
    {
        // Arrange - the sample's own Program, with nothing registered or overridden

        // Act - reading Services starts the sample's host, so DarkerValidationHostedService runs
        var services = factory.Services;
        var validators = services.GetServices<IAmAPipelineValidator>().ToList();
        var result = services.ValidateAll();

        // Assert
        validators.ShouldNotBeEmpty("ValidatePipelines() should be in the sample's registration chain");
        result.IsValid.ShouldBeTrue(
            string.Join(Environment.NewLine, result.Errors.Select(error => $"[{error.RuleId}] {error.Source}: {error.Message}")));
    }
}

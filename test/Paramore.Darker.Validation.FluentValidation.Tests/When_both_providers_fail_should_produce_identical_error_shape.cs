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

using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker;
using Paramore.Darker.Validation;
using Paramore.Darker.Validation.DataAnnotations;
using Paramore.Darker.Validation.FluentValidation.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Validation.FluentValidation.Tests;

public class CrossProviderIdenticalErrorShapeTests
{
    [Fact]
    public void When_both_providers_fail_should_produce_identical_error_shape()
    {
        // Arrange — FV side: empty Name violates NotEmpty() rule in FvTestQueryValidator
        var INVALID_NAME = string.Empty;
        var fvQuery = new FvTestQuery { Name = INVALID_NAME, Age = 18 };

        var serviceProvider = new ServiceCollection()
            .AddScoped<IValidator<FvTestQuery>, FvTestQueryValidator>()
            .BuildServiceProvider();

        // Decorator is closed over IQuery<TResult> at pipeline runtime (PipelineBuilder:253)
        var fvDecorator = new FluentValidationQueryValidatorDecorator<IQuery<FvTestQuery.Result>, FvTestQuery.Result>(serviceProvider);

        FvTestQuery.Result FvNext(IQuery<FvTestQuery.Result> q) => new FvTestQuery.Result();
        FvTestQuery.Result FvFallback(IQuery<FvTestQuery.Result> q) => new FvTestQuery.Result();

        // Arrange — DA side: empty Name violates [Required] on CrossProviderDaQuery
        var daQuery = new CrossProviderDaQuery { Name = INVALID_NAME };

        // Decorator is closed over IQuery<TResult> at pipeline runtime (PipelineBuilder:253)
        var daDecorator = new DataAnnotationsQueryValidatorDecorator<IQuery<CrossProviderDaQuery.Result>, CrossProviderDaQuery.Result>();

        CrossProviderDaQuery.Result DaNext(IQuery<CrossProviderDaQuery.Result> q) => new CrossProviderDaQuery.Result();
        CrossProviderDaQuery.Result DaFallback(IQuery<CrossProviderDaQuery.Result> q) => new CrossProviderDaQuery.Result();

        // Act — both decorators throw on the invalid query
        var fvException = Should.Throw<QueryValidationException>(
            () => fvDecorator.Execute(fvQuery, FvNext, FvFallback));

        var daException = Should.Throw<QueryValidationException>(
            () => daDecorator.Execute(daQuery, DaNext, DaFallback));

        // Assert — both Errors collections have the same shape: IReadOnlyCollection<QueryValidationError>
        fvException.Errors.ShouldBeAssignableTo<IReadOnlyCollection<QueryValidationError>>();
        daException.Errors.ShouldBeAssignableTo<IReadOnlyCollection<QueryValidationError>>();

        // Assert — FV Name error: PropertyName and ErrorMessage are populated (FR6 core shape)
        var fvNameError = fvException.Errors.Single(e => e.PropertyName == "Name");
        fvNameError.PropertyName.ShouldNotBeNullOrWhiteSpace();
        fvNameError.ErrorMessage.ShouldNotBeNullOrWhiteSpace();

        // Assert — DA Name error: PropertyName and ErrorMessage are populated (FR6 core shape)
        var daNameError = daException.Errors.Single(e => e.PropertyName == "Name");
        daNameError.PropertyName.ShouldNotBeNullOrWhiteSpace();
        daNameError.ErrorMessage.ShouldNotBeNullOrWhiteSpace();

        // Assert — documented shape difference: DataAnnotations has no AttemptedValue/ErrorCode concept
        daNameError.AttemptedValue.ShouldBeNull();
        daNameError.ErrorCode.ShouldBeNull();
    }
}

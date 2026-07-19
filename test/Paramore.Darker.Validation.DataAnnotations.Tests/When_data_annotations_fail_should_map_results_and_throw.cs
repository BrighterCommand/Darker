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

using System.Linq;
using Paramore.Darker;
using Paramore.Darker.Validation;
using Paramore.Darker.Validation.DataAnnotations;
using Paramore.Darker.Validation.DataAnnotations.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Validation.DataAnnotations.Tests;

public class DataAnnotationsQueryValidatorDecoratorInvalidQueryTests
{
    [Fact]
    public void When_data_annotations_fail_should_map_results_and_throw()
    {
        // Arrange — a query that violates two distinct constraints: Name is empty ([Required]), Age is out of range ([Range(1,120)])
        var INVALID_NAME = string.Empty;
        const int INVALID_AGE = 0;
        var query = new DaTestQuery { Name = INVALID_NAME, Age = INVALID_AGE };

        // The decorator is closed over IQuery<TResult> at pipeline runtime (see PipelineBuilder:253)
        var decorator = new DataAnnotationsQueryValidatorDecorator<IQuery<DaTestQuery.Result>, DaTestQuery.Result>();

        var nextCallCount = 0;
        DaTestQuery.Result Next(IQuery<DaTestQuery.Result> q)
        {
            nextCallCount++;
            return new DaTestQuery.Result();
        }
        DaTestQuery.Result Fallback(IQuery<DaTestQuery.Result> q) => new DaTestQuery.Result();

        // Act
        var exception = Should.Throw<QueryValidationException>(
            () => decorator.Execute(query, Next, Fallback));

        // Assert — two failures are surfaced (FR6: the whole collection, not just the first)
        exception.Errors.Count.ShouldBe(2);

        var errors = exception.Errors.ToList();
        var propertyNames = errors.Select(e => e.PropertyName).ToList();
        propertyNames.ShouldContain("Name");
        propertyNames.ShouldContain("Age");

        // Assert — DataAnnotations has no AttemptedValue or ErrorCode concept; both must be null
        var nameError = errors.Single(e => e.PropertyName == "Name");
        nameError.ErrorMessage.ShouldNotBeNullOrWhiteSpace();
        nameError.AttemptedValue.ShouldBeNull();
        nameError.ErrorCode.ShouldBeNull();

        var ageError = errors.Single(e => e.PropertyName == "Age");
        ageError.ErrorMessage.ShouldNotBeNullOrWhiteSpace();
        ageError.AttemptedValue.ShouldBeNull();
        ageError.ErrorCode.ShouldBeNull();

        // Assert — next was never invoked (pipeline short-circuits on failure)
        nextCallCount.ShouldBe(0);
    }
}

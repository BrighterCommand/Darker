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
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests.Specifications;

public class PredicateThrowsRuleIdTests
{
    [Fact]
    public void When_predicate_throws_should_report_evaluation_failed_rule_id()
    {
        // Arrange
        var specification = new Specification<string>(
            _ => throw new InvalidOperationException("boom"),
            s => new ValidationError(ValidationSeverity.Warning, s, "never used"));
        var collector = new ValidationResultCollector<string>();

        // Act
        var satisfied = specification.IsSatisfiedBy("subject");
        var errors = specification.Accept(collector).Select(r => r.Error!).ToList();

        // Assert
        satisfied.ShouldBeFalse();
        var error = errors.ShouldHaveSingleItem();
        error.RuleId.ShouldBe(ValidationRuleIds.EvaluationFailed);
        ValidationRuleIds.EvaluationFailed.ShouldBe(new ValidationRuleId("RuleEvaluationFailed"));
        error.Severity.ShouldBe(ValidationSeverity.Error);
        error.Message.ShouldContain("boom");
    }

    [Fact]
    public void When_error_built_with_positional_members_only_should_have_no_rule_id()
    {
        // Arrange / Act
        var error = new ValidationError(ValidationSeverity.Error, "source", "message");

        // Assert
        error.RuleId.ShouldBeNull();
    }
}

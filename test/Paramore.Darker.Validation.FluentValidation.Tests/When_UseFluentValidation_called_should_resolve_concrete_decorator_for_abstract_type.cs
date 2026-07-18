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
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Validation;
using Paramore.Darker.Validation.FluentValidation.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Validation.FluentValidation.Tests;

/// <summary>
/// Verifies that <c>UseFluentValidation()</c> registers the abstract→concrete open-generic
/// mappings so DI resolves the FluentValidation decorator when the pipeline asks for the
/// abstract type closed over <c>IQuery&lt;TResult&gt;</c>.
/// </summary>
public class UseFluentValidationRegistrationTests : IDisposable
{
    private readonly ServiceProvider _provider;

    public UseFluentValidationRegistrationTests()
    {
        //Arrange — shared: register Darker with UseFluentValidation(); no handlers needed
        var services = new ServiceCollection();
        services.AddDarker().UseFluentValidation();
        _provider = services.BuildServiceProvider();
    }

    [Fact]
    public void When_UseFluentValidation_called_should_resolve_concrete_decorator_for_abstract_type()
    {
        //Arrange — closed abstract sync type as PipelineBuilder requests it (PipelineBuilder.cs:253)
        var syncAbstractType = typeof(ValidateQueryDecorator<,>)
            .MakeGenericType(typeof(IQuery<FvTestQuery.Result>), typeof(FvTestQuery.Result));

        //Act
        var instance = _provider.GetService(syncAbstractType);

        //Assert — DI resolved the concrete FluentValidation decorator, not the abstract
        instance.ShouldBeOfType<FluentValidationQueryValidatorDecorator<IQuery<FvTestQuery.Result>, FvTestQuery.Result>>();
    }

    [Fact]
    public void When_UseFluentValidation_called_should_resolve_concrete_async_decorator_for_abstract_async_type()
    {
        //Arrange — closed abstract async type as PipelineBuilder requests it (PipelineBuilder.cs:404)
        var asyncAbstractType = typeof(ValidateQueryDecoratorAsync<,>)
            .MakeGenericType(typeof(IQuery<FvTestQuery.Result>), typeof(FvTestQuery.Result));

        //Act
        var instance = _provider.GetService(asyncAbstractType);

        //Assert — DI resolved the concrete async FluentValidation decorator, not the abstract
        instance.ShouldBeOfType<FluentValidationQueryValidatorDecoratorAsync<IQuery<FvTestQuery.Result>, FvTestQuery.Result>>();
    }

    [Fact]
    public void When_UseFluentValidation_called_with_null_builder_should_throw_ArgumentNullException()
    {
        //Arrange
        IDarkerHandlerBuilder nullBuilder = null!;

        //Act & Assert
        Should.Throw<ArgumentNullException>(() => nullBuilder.UseFluentValidation());
    }

    public void Dispose() => _provider.Dispose();
}

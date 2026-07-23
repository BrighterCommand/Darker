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
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Paramore.Darker.Caching.Tests.TestDoubles;
using Paramore.Darker.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Caching.Tests;

/// <summary>
/// Proves that <c>AddCaching()</c> and <c>AddCaching(Action{CachingOptions})</c> register the
/// caching decorators and wire up the correct <see cref="ICacheKeyGenerator"/>: the parameterless
/// path uses <see cref="DefaultCacheKeyGenerator"/>; the options-callback path honours a custom
/// generator supplied by the caller and the generator is used end-to-end through the pipeline.
/// </summary>
public sealed class AddCachingRegistrationTests
{
    [Fact]
    public void When_AddCaching_called_should_register_decorators_and_key_generator()
    {
        // Arrange — build a ServiceCollection with the parameterless AddCaching() overload
        var services = new ServiceCollection();
        services.AddDarker().AddCaching();

        // Act — build the provider and resolve ICacheKeyGenerator
        using var provider = services.BuildServiceProvider();
        var keyGenerator = provider.GetRequiredService<ICacheKeyGenerator>();

        // Assert — ICacheKeyGenerator resolves as the default implementation
        keyGenerator.ShouldBeOfType<DefaultCacheKeyGenerator>(
            "parameterless AddCaching() must register DefaultCacheKeyGenerator as ICacheKeyGenerator");

        // Assert — both decorator open-generic types are present in the service collection
        services.Any(sd => sd.ServiceType == typeof(CacheableQueryDecoratorAsync<,>)).ShouldBeTrue(
            "AddCaching() must register CacheableQueryDecoratorAsync<,> as a decorator");
        services.Any(sd => sd.ServiceType == typeof(CacheableQueryDecorator<,>)).ShouldBeTrue(
            "AddCaching() must register CacheableQueryDecorator<,> as a decorator");
    }

    [Fact]
    public async Task When_AddCaching_called_with_custom_key_generator_should_use_custom_generator()
    {
        // Arrange — supply a custom ICacheKeyGenerator via the options callback
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(LoggerFactory.Create(_ => { }));
        services.AddSingleton<HandlerCallCounter>();
        services.AddHybridCache();

        var customKeyGenerator = new CustomKeyGenerator();
        services
            .AddDarker()
            .AddHandlersFromAssemblies(typeof(CacheTestQueryHandlerAsync).Assembly)
            .AddCaching(o => o.KeyGenerator = customKeyGenerator);

        // Act — build provider and verify the custom instance is resolved
        await using var provider = services.BuildServiceProvider();
        var keyGenerator = provider.GetRequiredService<ICacheKeyGenerator>();

        // Assert — the exact custom instance is returned from the container
        keyGenerator.ShouldBeSameAs(customKeyGenerator,
            "AddCaching(o => o.KeyGenerator = ...) must register the supplied instance as ICacheKeyGenerator");

        // Act — execute a cacheable query end-to-end to prove the custom generator was used
        var queryProcessor = provider.GetRequiredService<IQueryProcessor>();
        var query = new CacheTestQuery { Payload = "custom-key-test" };
        await queryProcessor.ExecuteAsync(query);

        // Assert — the custom generator was invoked during pipeline execution
        customKeyGenerator.GenerateKeyCallCount.ShouldBe(1,
            "the custom ICacheKeyGenerator must be called exactly once when the query flows through the caching decorator");
    }
}

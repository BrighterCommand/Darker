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

using System;
using Microsoft.Extensions.DependencyInjection;

namespace Paramore.Darker.Extensions.DependencyInjection
{
    /// <summary>
    /// Owns a single per-query child <see cref="IServiceScope"/> from which Scoped and Transient
    /// components are resolved, so that those components and their disposable dependencies are torn
    /// down when the scope is disposed. The scope is created from the captured provider's
    /// <see cref="IServiceScopeFactory"/>, which yields a correctly-rooted scope even when the
    /// captured provider is the root container (the default Singleton <c>QueryProcessor</c>).
    /// </summary>
    internal sealed class ServiceProviderLifetimeScope(IServiceProvider serviceProvider) : IDisposable
    {
        private readonly IServiceScope _scope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope();

        public object Resolve(Type componentType) => _scope.ServiceProvider.GetService(componentType);

        public void Dispose() => _scope.Dispose();
    }
}

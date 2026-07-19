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

namespace Paramore.Darker.Validation.DataAnnotations.Tests.TestDoubles;

/// <summary>
/// Records whether the handler body was entered during a query execution.
/// Register this as a singleton in the <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection"/>
/// and inject it into <see cref="DaTestQueryHandlerAsync"/> via constructor injection so that
/// end-to-end tests can assert on an instance, avoiding fragile static mutable state that can
/// cause flaky failures under xUnit's parallel test execution.
/// </summary>
public sealed class HandlerExecutionRecorder
{
    /// <summary>Gets a value indicating whether the handler body was entered.</summary>
    public bool Executed { get; private set; }

    /// <summary>Records that the handler body was entered.</summary>
    public void Record() => Executed = true;

    /// <summary>Resets <see cref="Executed"/> to <c>false</c> before a new test scenario.</summary>
    public void Reset() => Executed = false;
}

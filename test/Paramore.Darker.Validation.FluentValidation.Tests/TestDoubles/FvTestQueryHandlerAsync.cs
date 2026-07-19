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

using System.Threading;
using System.Threading.Tasks;
using Paramore.Darker.Validation;

namespace Paramore.Darker.Validation.FluentValidation.Tests.TestDoubles;

/// <summary>
/// A test-double async handler for <see cref="FvTestQuery"/> whose <c>ExecuteAsync</c> method
/// carries <see cref="ValidateQueryAttributeAsync"/> so that the validation decorator is wired
/// into the pipeline automatically.
/// </summary>
/// <remarks>
/// Must be <c>public</c> so that <c>AddHandlersFromAssemblies</c> discovers it via
/// <c>Assembly.GetExportedTypes()</c> when scanning the test assembly in end-to-end tests.
/// The static <see cref="HandlerExecuted"/> flag lets end-to-end tests assert whether the
/// handler body ran or was short-circuited by the validation decorator.
/// Reset the flag via <see cref="Reset"/> before each test scenario.
/// </remarks>
public sealed class FvTestQueryHandlerAsync : QueryHandlerAsync<FvTestQuery, FvTestQuery.Result>
{
    /// <summary>Gets a value indicating whether the handler body was entered during the last execution.</summary>
    public static bool HandlerExecuted { get; private set; }

    /// <summary>Resets <see cref="HandlerExecuted"/> to <c>false</c> before a new test scenario.</summary>
    public static void Reset() => HandlerExecuted = false;

    /// <inheritdoc />
    [ValidateQueryAttributeAsync(1)]
    public override Task<FvTestQuery.Result> ExecuteAsync(
        FvTestQuery query,
        CancellationToken cancellationToken = default)
    {
        HandlerExecuted = true;
        return Task.FromResult(new FvTestQuery.Result { Value = query.Name });
    }
}

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

using System.ComponentModel.DataAnnotations;
using Paramore.Darker;

namespace Paramore.Darker.Validation.FluentValidation.Tests.TestDoubles;

/// <summary>
/// A DataAnnotations-annotated query double used by the cross-provider shape test.
/// Mirrors the <c>Name</c> property constrained in <see cref="FvTestQueryValidator"/> so
/// an empty <c>Name</c> triggers a failure under both providers.
/// </summary>
public sealed class CrossProviderDaQuery : IQuery<CrossProviderDaQuery.Result>
{
    [Required]
    public string Name { get; set; } = string.Empty;

    public sealed class Result
    {
        public string Value { get; set; } = string.Empty;
    }
}

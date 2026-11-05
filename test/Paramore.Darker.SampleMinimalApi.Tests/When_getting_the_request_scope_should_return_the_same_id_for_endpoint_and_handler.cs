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
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc.Testing;
using Paramore.Darker.SampleMinimalApi.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.SampleMinimalApi.Tests;

[Collection("SampleMinimalApi")]
public class RequestScopeEndpointTests(WebApplicationFactory<Program> factory)
{
    [Fact]
    public async Task When_getting_the_request_scope_should_return_the_same_id_for_endpoint_and_handler()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/request-scope", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<RequestScopeResponse>(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        body.ShouldNotBeNull();
        body.Endpoint.ShouldNotBe(Guid.Empty);
        body.Handler.ShouldBe(body.Endpoint);
    }
}

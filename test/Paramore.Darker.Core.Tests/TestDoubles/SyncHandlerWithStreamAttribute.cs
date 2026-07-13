// Copyright (c) 2025, Ian Cooper
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without modification, are permitted provided that the
// following conditions are met:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the
// Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING
// BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>
    /// A sync handler that incorrectly uses a StreamQueryHandlerAttribute on Execute.
    /// This should cause a ConfigurationException at pipeline build time.
    /// </summary>
    internal class SyncHandlerWithStreamAttribute : QueryHandler<SyncTestQuery, SyncTestQuery.Result>
    {
        [StreamStepEvent(1)]
        public override SyncTestQuery.Result Execute(SyncTestQuery query)
            => new SyncTestQuery.Result { Value = query.Id };
    }
}

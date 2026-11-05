using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>A hosted service that records when its StartAsync runs.</summary>
    public sealed class RecordingHostedService : IHostedService
    {
        private readonly StartupSequence _sequence;

        public RecordingHostedService(StartupSequence sequence) => _sequence = sequence;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _sequence.Record("other service StartAsync");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}

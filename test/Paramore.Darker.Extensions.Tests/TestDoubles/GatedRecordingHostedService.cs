using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>
    /// A hosted service whose StartAsync records its entry, signals <see cref="Entered"/>, then stays incomplete
    /// until <see cref="Release"/> is completed, forcing the host onto its concurrent start path.
    /// </summary>
    public sealed class GatedRecordingHostedService : IHostedService
    {
        private readonly StartupSequence _sequence;

        public GatedRecordingHostedService(StartupSequence sequence) => _sequence = sequence;

        /// <summary>Completed once StartAsync has been entered.</summary>
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>Completing this lets StartAsync finish.</summary>
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>Whether validation had already been recorded when StartAsync was entered.</summary>
        public bool ValidationRecordedBeforeEntry { get; private set; }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            ValidationRecordedBeforeEntry = System.Array.IndexOf(_sequence.Entries, "validation") >= 0;
            _sequence.Record("gated service StartAsync");
            Entered.TrySetResult();
            await Release.Task;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}

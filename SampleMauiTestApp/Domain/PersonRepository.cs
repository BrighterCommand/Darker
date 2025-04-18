namespace SampleMauiTestApp.Domain
{
    public sealed class PersonRepository
    {
        private static readonly IReadOnlyDictionary<int, string> Db = new Dictionary<int, string>
        {
            {1, "Alan Turing"},
            {2, "Donald Knuth"},
            {3, "Edsger W. Dijkstra"},
            {4, "John von Neumann"},
            {5, "Dennis Ritchie"},
            {6, "Ken Thompson"},
            {7, "Tim Berners-Lee"},
            {8, "Claude Shannon"}
        };

        public async Task<IReadOnlyDictionary<int, string>> GetAll(CancellationToken cancellationToken = new())
        {
            // fake some io-bound activity. Need to call ConfigureAwait(false) to avoid deadlocks in UI thread.
            await FakeSomeActivity(cancellationToken).ConfigureAwait(false);

            return Db;
        }

        public async Task<string> GetNameById(int id, CancellationToken cancellationToken = new())
        {
            // fake some io-bound activity. Need to call ConfigureAwait(false) to avoid deadlocks in UI thread.
            await FakeSomeActivity(cancellationToken).ConfigureAwait(false);

            return Db[id];
        }

        private async Task FakeSomeActivity(CancellationToken cancellationToken = new())
        {
            var random = new Random();

            // fake some io-bound activity. Need to call ConfigureAwait(false) to avoid deadlocks in UI thread.
            await Task.Delay(random.Next(50, 150), cancellationToken).ConfigureAwait(false);

            // there is a 10% chance that something goes terribly wrong
            if (random.NextDouble() < 0.10)
                throw new SomethingWentTerriblyWrongException();
        }
    }
}
using System;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    internal sealed class DatedQuery : IQuery<string>
    {
        public DateTime Date { get; }

        public DatedQuery(DateTime date)
        {
            Date = date;
        }
    }
}

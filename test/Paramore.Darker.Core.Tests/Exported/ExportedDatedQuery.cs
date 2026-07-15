using System;

namespace Paramore.Darker.Core.Tests.Exported
{
    public class ExportedDatedQuery : IQuery<string>
    {
        public DateTime Date { get; }

        public ExportedDatedQuery(DateTime date)
        {
            Date = date;
        }
    }
}

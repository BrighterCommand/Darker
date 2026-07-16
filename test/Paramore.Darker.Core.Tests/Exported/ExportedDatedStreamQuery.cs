using System;

namespace Paramore.Darker.Core.Tests.Exported
{
    public class ExportedDatedStreamQuery : IStreamQuery<string>
    {
        public DateTime Date { get; }

        public ExportedDatedStreamQuery(DateTime date)
        {
            Date = date;
        }
    }
}

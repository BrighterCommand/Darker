using Paramore.Darker;

namespace SampleApi.Ports
{
    public sealed class GetRandomQuote : IQuery<GetRandomQuote.Result>
    {
        public sealed class Result
        {
            public string Quote { get; }
            public string Author { get; }

            public Result(string quote, string author)
            {
                Quote = quote;
                Author = author;
            }
        }
    }
}

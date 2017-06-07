using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Paramore.Darker;
using SampleApi.Ports.Queries;

namespace SampleApi.Controllers
{
    [Route("api/quotes")]
    public class QuotesController : Controller
    {
        private readonly IQueryProcessor _queryProcessor;

        public QuotesController(IQueryProcessor queryProcessor)
        {
            _queryProcessor = queryProcessor;
        }

        [HttpGet]
        public async Task<string> Get()
        {
            var quote = await _queryProcessor.ExecuteRemoteAsync(new GetRandomQuote());

            return $"\"{quote.Quote}\" – {quote.Author}";
        }
    }
}
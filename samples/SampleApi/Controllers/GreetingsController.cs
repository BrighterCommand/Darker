using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Paramore.Darker;
using SampleApi.Ports;

namespace SampleApi.Controllers
{
    [Route("api/greetings")]
    public class GreetingsController : Controller
    {
        private readonly IQueryProcessor _queryProcessor;

        public GreetingsController(IQueryProcessor queryProcessor)
        {
            _queryProcessor = queryProcessor;
        }

        [HttpGet("aws/{name}")]
        public async Task<string> GetAws(string name)
        {
            return await _queryProcessor.ExecuteAsync(new GetAwsGreeting(name));
        }
        
        [HttpGet("azure/{name}")]
        public async Task<string> GetAzure(string name)
        {
            return await _queryProcessor.ExecuteAsync(new GetAzureGreeting(name));
        }
    }
}
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Paramore.Darker;
using SampleApi.Ports.Queries;

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

        [HttpGet("{name}")]
        public async Task<string> Get(string name)
        {
            return await _queryProcessor.ExecuteAsync(new GetGreeting(name));
        }

        [HttpGet("{name}/aws")]
        public async Task<string> GetAws(string name)
        {
            return await _queryProcessor.ExecuteRemoteAsync(new GetAwsGreeting(name));
        }
        
        [HttpGet("{name}/azure")]
        public async Task<string> GetAzure(string name)
        {
            return await _queryProcessor.ExecuteRemoteAsync(new GetAzureGreeting(name));
        }
    }
}
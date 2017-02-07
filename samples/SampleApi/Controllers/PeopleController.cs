using System.Collections.Generic;
using System.Threading.Tasks;
using Darker;
using Microsoft.AspNetCore.Mvc;
using SampleApi.Ports;

namespace SampleApi.Controllers
{
    [Route("api/people")]
    public class PeopleController : Controller
    {
        private readonly IQueryProcessor _queryProcessor;

        public PeopleController(IQueryProcessor queryProcessor)
        {
            _queryProcessor = queryProcessor;
        }

        // GET api/people
        [HttpGet]
        public async Task<IReadOnlyDictionary<int, string>> Get()
        {
            var result = await _queryProcessor.ExecuteAsync(new GetPeopleQuery());
            return result.People;
        }

        // GET api/people/5
        [HttpGet("{id}")]
        public async Task<string> Get(int id)
        {
            var result = await _queryProcessor.ExecuteAsync(new GetPersonQuery(id));
            return result.Name;
        }
    }
}

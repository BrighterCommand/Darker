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
            return await _queryProcessor.ExecuteAsync(new GetPeopleQuery());
        }

        // GET api/people/5
        [HttpGet("{id}")]
        public async Task<string> Get(int id)
        {
            return await _queryProcessor.ExecuteAsync(new GetPersonNameQuery(id));
        }
    }
}

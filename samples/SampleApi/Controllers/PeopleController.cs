using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Paramore.Darker;
using SampleApi.Ports.Queries;

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
            return await _queryProcessor.ExecuteAsync(new GetPeople());
        }

        // GET api/people/5
        [HttpGet("{id}")]
        public async Task<string> Get(int id)
        {
            return await _queryProcessor.ExecuteAsync(new GetPersonName(id));
        }
    }
}
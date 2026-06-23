using Paramore.Darker;
using Paramore.Darker.Extensions.DependencyInjection;
using SampleMinimalApi;
using SampleMinimalApi.QueryHandlers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDarker()
    .AddHandlersFromAssemblies(typeof(GetPeopleQuery).Assembly)
    .AddJsonQueryLogging()
    .AddResiliencePipelines(DarkerSettings.ConfigureResiliencePipelines());

var app = builder.Build();

app.MapGet("/people",
    async (IQueryProcessor queryProcessor) => await queryProcessor.ExecuteAsync(new GetPeopleQuery()));


app.MapGet("/people/{id:int}",
    async (IQueryProcessor queryProcessor, int id) => await queryProcessor.ExecuteAsync(new GetPersonNameQuery(id)));


// Demonstrates the retry pipeline recovering from a transient failure: the handler throws on its
// first attempt and succeeds when the pipeline retries it.
app.MapGet("/flaky",
    async (IQueryProcessor queryProcessor) => await queryProcessor.ExecuteAsync(new GetFlakyResultQuery()));


app.Run();
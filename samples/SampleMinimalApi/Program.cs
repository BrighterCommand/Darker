using Paramore.Darker;
using Paramore.Darker.Extensions.DependencyInjection;
using SampleMinimalApi;
using SampleMinimalApi.Domain;
using SampleMinimalApi.QueryHandlers;

var builder = WebApplication.CreateBuilder(args);

// A Scoped service stands in for something like an EF Core DbContext: one instance per request.
builder.Services.AddScoped<RequestCorrelation>();

// Scoped handlers plus AddDarkerRequestScope() let a handler share the request's Scoped services.
builder.Services.AddDarker(options => options.HandlerLifetime = ServiceLifetime.Scoped)
    .AddHandlersFromAssemblies(typeof(GetPeopleQuery).Assembly)
    .AddJsonQueryLogging()
    .AddResiliencePipelines(DarkerSettings.ConfigureResiliencePipelines());
builder.Services.AddDarkerRequestScope();

var app = builder.Build();

app.MapGet("/people",
    async (IQueryProcessor queryProcessor) => await queryProcessor.ExecuteAsync(new GetPeopleQuery()));


app.MapGet("/people/{id:int}",
    async (IQueryProcessor queryProcessor, int id) => await queryProcessor.ExecuteAsync(new GetPersonNameQuery(id)));


// Demonstrates the retry pipeline recovering from a transient failure: the handler throws on its
// first attempt and succeeds when the pipeline retries it.
app.MapGet("/flaky",
    async (IQueryProcessor queryProcessor) => await queryProcessor.ExecuteAsync(new GetFlakyResultQuery()));


// Demonstrates the request scope: the endpoint and the handler see the same Scoped RequestCorrelation,
// so the two ids match. Without AddDarkerRequestScope() the handler gets its own scope and they differ.
app.MapGet("/request-scope",
    async (IQueryProcessor queryProcessor, RequestCorrelation correlation) =>
        new
        {
            Endpoint = correlation.Id,
            Handler = await queryProcessor.ExecuteAsync(new GetRequestCorrelationQuery())
        });


app.Run();
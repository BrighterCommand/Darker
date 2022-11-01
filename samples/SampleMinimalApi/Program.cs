using Paramore.Darker;
using Paramore.Darker.AspNetCore;
using Paramore.Darker.Policies;
using Paramore.Darker.QueryLogging;
using SampleMinimalApi;
using SampleMinimalApi.QueryHandlers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDarker()
    .AddHandlersFromAssemblies(typeof(GetPeopleQuery).Assembly)
    .AddJsonQueryLogging()
    .AddPolicies(DarkerSettings.ConfigurePolicies());

var app = builder.Build();

app.MapGet("/people",
    async (IQueryProcessor queryProcessor) => await queryProcessor.ExecuteAsync(new GetPeopleQuery()));


app.MapGet("/people/{id:int}",
    async (IQueryProcessor queryProcessor, int id) => await queryProcessor.ExecuteAsync(new GetPersonNameQuery(id)));


app.Run();
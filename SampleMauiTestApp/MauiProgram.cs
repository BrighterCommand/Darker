using Microsoft.Extensions.Logging;
using Paramore.Darker.AspNetCore;
using Paramore.Darker.Policies;
using Paramore.Darker.QueryLogging;
using SampleMauiTestApp.QueryHandlers;

namespace SampleMauiTestApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureDarker()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }

        public static MauiAppBuilder ConfigureDarker(this MauiAppBuilder appBuilder)
        {
            appBuilder.Services.AddDarker()
                .AddHandlersFromAssemblies(typeof(GetPeopleQuery).Assembly)
                .AddJsonQueryLogging()
                .AddPolicies(DarkerSettings.ConfigurePolicies());

            return appBuilder;
        }
    }
}

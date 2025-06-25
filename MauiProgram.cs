using Microsoft.Extensions.Logging;
using PCANAppM.Services;              // ← for ICanBusService & CanBusService

namespace PCANAppMaui
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("InriaSans-Regular.ttf", "InriaSansRegular");
                });

            // ← register your CAN-bus service as a singleton
            builder.Services.AddSingleton<ICanBusService, CanBusService>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}

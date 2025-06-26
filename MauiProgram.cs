#if WINDOWS
using Microsoft.Extensions.Logging;
using LocalizationResourceManager.Maui;
using PCANAppM.Resources.Languages;
using System.Globalization;
using Syncfusion.Maui.Core.Hosting; 
using PCANAppM.Services;

namespace PCANAppM
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureSyncfusionCore()
                .UseLocalizationResourceManager(settings =>
                {
                    settings.AddResource(PCANAppM.Resources.Languages.AppResources.ResourceManager);
                })
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("Barlow-Regular.ttf", "BarlowRegular");
                });

            builder.Services.AddSingleton<MainPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif
            // Register the localization resource manager and services
            builder.Services.AddSingleton<ICanBusService, CanBusService>();
            return builder.Build();
        }
    }
}
#endif

#if WINDOWS
using Microsoft.Extensions.Logging;
using LocalizationResourceManager.Maui;
using PCANAppM.Resources.Languages;
using System.Globalization;
using Syncfusion.Maui.Core.Hosting; 
using PCANAppM.Services;

namespace PCANAppM
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureSyncfusionCore()
                .UseLocalizationResourceManager(settings =>
                {
                    settings.AddResource(PCANAppM.Resources.Languages.AppResources.ResourceManager);
                })
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("Barlow-Regular.ttf", "BarlowRegular");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif
            builder.Services.AddSingleton<ICanBusService, CanBusService>();
            builder.Services.AddSingleton<ILocalizationResourceManager, LocalizationResourceManager.Maui.LocalizationResourceManager>();

            builder.Services.AddSingleton<MainPage>();
            builder.Services.AddTransient<Menu>();
            builder.Services.AddTransient<BAS>();
            builder.Services.AddTransient<KZV>();
            builder.Services.AddTransient<FTLS>();



            return builder.Build();
        }
    }
}
#endif

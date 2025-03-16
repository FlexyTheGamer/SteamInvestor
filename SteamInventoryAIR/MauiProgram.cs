using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using SteamInventoryAIR.Interfaces;
using SteamInventoryAIR.Services;
using SteamInventoryAIR.ViewModels;

using System.Text.Json;
using Microsoft.Maui.ApplicationModel;


namespace SteamInventoryAIR
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
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            builder.UseMauiCommunityToolkit();

            // Register services
            builder.Services.AddSingleton<ISteamAuthService, SteamAuthService>();

            // Register ViewModels
            builder.Services.AddTransient<LoginViewModel>();
            builder.Services.AddTransient<MainViewModel>();

            // Register Pages
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<AppShell>();


            //#if DEBUG
            //    		builder.Logging.AddDebug();
            //#endif

            return builder.Build();
        }
    }
}

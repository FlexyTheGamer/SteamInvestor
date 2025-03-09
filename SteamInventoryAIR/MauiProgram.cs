﻿using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;

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

            //?????????????? Set LoginPage as the initial page ????????? - Probabbly defines some sort of service object for the form
            builder.Services.AddSingleton<LoginPage>();

            //#if DEBUG
            //    		builder.Logging.AddDebug();
            //#endif

            return builder.Build();
        }
    }
}

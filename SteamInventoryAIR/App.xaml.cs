using SteamInventoryAIR.Services;
using SteamInventoryAIR.ViewModels;

namespace SteamInventoryAIR
{
    public partial class App : Application
    {
        public App(IServiceProvider serviceProvider)
        {
            InitializeComponent();

            // Load environment variables for debugging
            EnvironmentService.LoadEnvironmentVariables();

            //--------------------Testing purposes
            ////Added for MVVM Architecture
            //Routing.RegisterRoute(nameof(MainPage), typeof(MainPage));

            //MainPage = new NavigationPage(new LoginPage(
            //    serviceProvider.GetService<LoginViewModel>()
            //));
            //--------------------Testing purposes

            // Use AppShell consistently
            MainPage = new AppShell();
        }
    }
}
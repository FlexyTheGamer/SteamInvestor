using SteamInventoryAIR.ViewModels;

namespace SteamInventoryAIR
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            //MainPage = new AppShell(); - Old Project Introduction FormPage


            //Pre MVVM Architecture
            //MainPage = new LoginPage(); //Initially runs the specified form ?!


            //Added for MVVM Architecture
            Routing.RegisterRoute(nameof(MainPage), typeof(MainPage));

            MainPage = new NavigationPage(new MainPage
            {
                BindingContext = new BaseViewModel()
            });
        }
    }
}

namespace SteamInventoryAIR
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute(nameof(MainPage), typeof(MainPage));  //dependency injection for the LoginPage
        }
    }
}

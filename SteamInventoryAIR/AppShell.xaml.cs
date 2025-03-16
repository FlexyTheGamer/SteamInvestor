using System.Diagnostics;

namespace SteamInventoryAIR
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Register routes AFTER InitializeComponent
            Routing.RegisterRoute(nameof(MainPage), typeof(MainPage));  //dependency injection for the LoginPage ?????????????

        }

        // Move navigation to OnAppearing instead of constructor
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            try
            {
                // Navigate after shell is fully loaded
                await Current.GoToAsync("//login");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Navigation error: {ex.Message}");
            }
        }
    }
}

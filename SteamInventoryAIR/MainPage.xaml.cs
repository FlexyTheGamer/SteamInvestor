using SteamKit2;
using SteamKit2.Authentication;
using System.Diagnostics;

using SteamInventoryAIR.ViewModels;
using SteamInventoryAIR.Interfaces;

namespace SteamInventoryAIR
{
    public partial class MainPage : ContentPage
    {
        private readonly MainViewModel _viewModel;

        // Add this constructor
        [Obsolete("This constructor is only used by XAML previewer or Shell routing")]
        public MainPage()
        {
            InitializeComponent();
            Debug.WriteLine("Warning: MainPage initialized without auth service");

            // Create and assign the view model with null auth service temporarily
            _viewModel = new MainViewModel(null);
            BindingContext = _viewModel;
        }

        public MainPage(ISteamAuthService authService)
        {
            InitializeComponent();

            // Create and assign the view model with auth service
            _viewModel = new MainViewModel(authService);
            BindingContext = _viewModel;

            Debug.WriteLine("MainPage initialized with auth service");
        }


        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Connect Value/Volume toggle buttons
            BTN_ValueToggle.Clicked += (s, e) => {
                // Update visuals
                BTN_ValueToggle.BackgroundColor = Color.FromArgb("#CC2424");
                BTN_VolumeToggle.BackgroundColor = Color.FromArgb("#2A2E35");
                // Trigger command
                _viewModel.ToggleValueGraphCommand.Execute(null);
            };

            BTN_VolumeToggle.Clicked += (s, e) => {
                // Update visuals
                BTN_ValueToggle.BackgroundColor = Color.FromArgb("#2A2E35");
                BTN_VolumeToggle.BackgroundColor = Color.FromArgb("#CC2424");
                // Trigger command
                _viewModel.ToggleVolumeGraphCommand.Execute(null);
            };
        }

    }

}

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
    }

}

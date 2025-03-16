using SteamInventoryAIR.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SteamInventoryAIR.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        // Properties for inventory information
        private string _inventoryValue;
        public string InventoryValue
        {
            get => _inventoryValue;
            set => SetProperty(ref _inventoryValue, value);
        }

        private string _inventoryItemQuantity;
        public string InventoryItemQuantity
        {
            get => _inventoryItemQuantity;
            set => SetProperty(ref _inventoryItemQuantity, value);
        }

        // Track which tab is selected
        private int _selectedTabIndex;
        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set => SetProperty(ref _selectedTabIndex, value);
        }

        private string _welcomeMessage;
        public string WelcomeMessage
        {
            get => _welcomeMessage;
            set => SetProperty(ref _welcomeMessage, value);
        }

        private bool _isValueGraphSelected = true;
        public bool IsValueGraphSelected
        {
            get => _isValueGraphSelected;
            set => SetProperty(ref _isValueGraphSelected, value);
        }

        // Commands for tab navigation
        public ICommand SelectHomeTabCommand { get; }
        public ICommand SelectListTabCommand { get; }
        public ICommand SelectMonitoringTabCommand { get; }
        
        public ICommand ToggleValueGraphCommand { get; }
        public ICommand ToggleVolumeGraphCommand { get; }

        public MainViewModel(ISteamAuthService authService = null)
        {
            Title = "Steam Inventory";
            Debug.WriteLine("MainViewModel initialized");

            // Initialize with sample data
            InventoryValue = "1337,64 €";
            InventoryItemQuantity = "360";

            // Initialize commands
            SelectHomeTabCommand = new Command(() =>
            {
                Debug.WriteLine("Home tab selected");
                SelectedTabIndex = 0;
            });

            SelectListTabCommand = new Command(() =>
            {
                Debug.WriteLine("List tab selected");
                SelectedTabIndex = 1;
            });

            SelectMonitoringTabCommand = new Command(() =>
            {
                Debug.WriteLine("Monitoring tab selected");
                SelectedTabIndex = 2;
            });

            ToggleValueGraphCommand = new Command(() => {
                IsValueGraphSelected = true;
                Debug.WriteLine("Value graph selected");
            });

            ToggleVolumeGraphCommand = new Command(() => {
                IsValueGraphSelected = false;
                Debug.WriteLine("Volume graph selected");
            });

            // Set default tab
            SelectedTabIndex = 0;

            // If auth service provided, get the persona name
            if (authService != null)
            {
                LoadPersonaName(authService);
            }
        }

        // Add this method to load the persona name
        private async void LoadPersonaName(ISteamAuthService authService)
        {
            try
            {
                string personaName = await authService.GetPersonaNameAsync();
                if (!string.IsNullOrEmpty(personaName) && personaName != "Unknown User")
                {
                    WelcomeMessage = $"Hello, {personaName}!";
                    Debug.WriteLine($"Updated welcome message with persona name: {personaName}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading persona name: {ex.Message}");
            }
        }
    }
}

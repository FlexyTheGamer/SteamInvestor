using SteamInventoryAIR.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

using System.Collections.ObjectModel;
using SteamInventoryAIR.Models;

namespace SteamInventoryAIR.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly ISteamAuthService _authService;

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


        private ObservableCollection<InventoryItem> _inventoryItems;
        public ObservableCollection<InventoryItem> InventoryItems
        {
            get => _inventoryItems;
            set => SetProperty(ref _inventoryItems, value);
        }

        private bool _isLoadingInventory;
        public bool IsLoadingInventory
        {
            get => _isLoadingInventory;
            set => SetProperty(ref _isLoadingInventory, value);
        }

        private string _inventoryStatus;
        public string InventoryStatus
        {
            get => _inventoryStatus;
            set => SetProperty(ref _inventoryStatus, value);
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

            // Store the auth service
            _authService = authService;

            // Initialize collections
            InventoryItems = new ObservableCollection<InventoryItem>();

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
                // Load inventory when the tab is selected
                _ = LoadInventoryAsync();
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


        public async Task LoadInventoryAsync()
        {
            if (_authService == null)
            {
                Debug.WriteLine("LoadInventoryAsync: Auth service is not available");
                InventoryStatus = "Error: Auth service not available";
                return;
            }

            try
            {
                Debug.WriteLine("LoadInventoryAsync: Starting authenticated inventory load");
                IsLoadingInventory = true;
                InventoryStatus = "Loading inventory...";


                var items = await _authService.GetInventoryViaWebAPIAsync();

                Debug.WriteLine($"LoadInventoryAsync: Retrieved {items?.Count() ?? 0} items via authenticated session");

                // Update the ObservableCollection
                InventoryItems.Clear();
                if (items != null)
                {
                    foreach (var item in items)
                    {
                        InventoryItems.Add(item);
                    }
                }

                // Update inventory value and count
                if (items?.Any() == true)
                {
                    // If we have market values, use them. Otherwise use placeholder values.
                    if (items.Any(i => i.MarketValue > 0))
                    {
                        InventoryValue = $"{items.Sum(i => i.MarketValue):N2} €";
                    }
                    InventoryItemQuantity = items.Count().ToString();
                    InventoryStatus = $"Loaded {items.Count()} items";

                    // Debug output for all items
                    foreach (var item in items)
                    {
                        Debug.WriteLine($"Item: {item}");
                    }
                }
                else
                {
                    InventoryValue = "0.00 €";
                    InventoryItemQuantity = "0";
                    InventoryStatus = items?.Any() == false ? "No items found" : "Failed to load inventory";
                }

                Debug.WriteLine($"LoadInventoryAsync: Updated inventory values: {InventoryValue}, {InventoryItemQuantity}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LoadInventoryAsync: Error: {ex.Message}");
                InventoryStatus = $"Error: {ex.Message}";
            }
            finally
            {
                IsLoadingInventory = false;
            }
        }




    }
}

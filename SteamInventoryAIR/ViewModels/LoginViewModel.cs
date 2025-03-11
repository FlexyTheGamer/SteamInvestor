using System.Windows.Input;
using System.Diagnostics;
using SteamInventoryAIR.Interfaces;

namespace SteamInventoryAIR.ViewModels
{
    public class LoginViewModel : BaseViewModel   //Changed Public -> Internal (because of an error)
    {
        private readonly ISteamAuthService _authService;

        // Properties for traditional login
        private string? _username;
        public string? Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        private string? _password;
        public string? Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        private string? _authCode;
        public string? AuthCode
        {
            get => _authCode;
            set => SetProperty(ref _authCode, value);
        }

        // Property for session key login
        private string? _sessionKey;
        public string? SessionKey
        {
            get => _sessionKey;
            set => SetProperty(ref _sessionKey, value);
        }

        // Property for QR code login
        private string? _qrCodeUrl;
        public string? QrCodeUrl
        {
            get => _qrCodeUrl;
            set => SetProperty(ref _qrCodeUrl, value);
        }

        //Temperorarly? for testing login success
        private string _loginStatus;
        public string LoginStatus
        {
            get => _loginStatus;
            set => SetProperty(ref _loginStatus, value);
        }

        private string _profileName;
        public string ProfileName
        {
            get => _profileName;
            set => SetProperty(ref _profileName, value);
        }



        // Commands for login actions
        public ICommand TraditionalLoginCommand { get; }
        public ICommand SessionKeyLoginCommand { get; }
        public ICommand QrCodeLoginCommand { get; }
        public ICommand GenerateQrCodeCommand { get; }

        public LoginViewModel(ISteamAuthService authService)
        {
            Title = "Login";
            _authService = authService;

            TraditionalLoginCommand = new Command(async () => await ExecuteTraditionalLoginCommand());
            SessionKeyLoginCommand = new Command(async () => await ExecuteSessionKeyLoginCommand());
            QrCodeLoginCommand = new Command(async () => await ExecuteQrCodeLoginCommand());
            GenerateQrCodeCommand = new Command(async () => await ExecuteGenerateQrCodeCommand());
        }

        private async Task ExecuteTraditionalLoginCommand()
        {
            if (IsBusy)
                return;

            if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
                return;

            try
            {
                IsBusy = true;
                LoginStatus = "Logging in...";

                bool success = await _authService.LoginWithCredentialsAsync(Username, Password, AuthCode);

                if (success)
                {
                    // Get the actual Steam profile name
                    string profileName = await _authService.GetPersonaNameAsync();
                    LoginStatus = $"Successfully logged in as {profileName}";

                    // Reset form fields
                    Username = string.Empty;
                    Password = string.Empty;
                    AuthCode = string.Empty;

                    // TODO: Navigate to main page when ready
                    // await Shell.Current.GoToAsync("///MainPage");
                }
                else
                {
                    LoginStatus = "Login failed. Please check your credentials.";
                }
            }
            catch (Exception ex)
            {
                LoginStatus = $"Error: {ex.Message}";
                Debug.WriteLine($"Login error: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ExecuteSessionKeyLoginCommand()
        {
            if (IsBusy)
                return;

            if (string.IsNullOrEmpty(SessionKey))
                return;

            try
            {
                IsBusy = true;
                bool success = await _authService.LoginWithSessionKeyAsync(SessionKey);

                if (success)
                {
                    // Navigate to main page or trigger navigation event
                    // For now, we'll just reset the form
                    SessionKey = string.Empty;
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions
                Debug.WriteLine($"Session key login error: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ExecuteQrCodeLoginCommand()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                bool success = await _authService.LoginWithQRCodeAsync("dummy-token");

                if (success)
                {
                    // Navigate to main page or trigger navigation event
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions
                Debug.WriteLine($"QR login error: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ExecuteGenerateQrCodeCommand()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                string token = await _authService.GenerateQRCodeTokenAsync();
                QrCodeUrl = token; // In a real app, you would convert this to a QR code image
            }
            catch (Exception ex)
            {
                // Handle exceptions
                Debug.WriteLine($"QR code generation error: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}

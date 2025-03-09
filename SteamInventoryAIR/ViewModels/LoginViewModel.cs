using System.Windows.Input;
using SteamInventoryAIR.Services;
using System.Diagnostics;

namespace SteamInventoryAIR.ViewModels
{
    internal class LoginViewModel : BaseViewModel   //Changed Public -> Internal (because of an error)
    {
        private readonly ISteamAuthService _authService;

        // Properties for traditional login
        private string _username;
        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        private string _password;
        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        private string _authCode;
        public string AuthCode
        {
            get => _authCode;
            set => SetProperty(ref _authCode, value);
        }

        // Property for session key login
        private string _sessionKey;
        public string SessionKey
        {
            get => _sessionKey;
            set => SetProperty(ref _sessionKey, value);
        }

        // Property for QR code login
        private string _qrCodeUrl;
        public string QrCodeUrl
        {
            get => _qrCodeUrl;
            set => SetProperty(ref _qrCodeUrl, value);
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
                bool success = await _authService.LoginWithCredentialsAsync(Username, Password, AuthCode);

                if (success)
                {
                    // Navigate to main page or trigger navigation event
                    // For now, we'll just reset the form
                    Username = string.Empty;
                    Password = string.Empty;
                    AuthCode = string.Empty;
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions
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

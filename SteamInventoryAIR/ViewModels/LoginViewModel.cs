using System.Windows.Input;
using System.Diagnostics;
using SteamInventoryAIR.Interfaces;

using Microsoft.Maui.ApplicationModel;

using QRCoder;
using System.IO;



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

        private ImageSource _qrCodeImageSource;
        public ImageSource QrCodeImageSource
        {
            get => _qrCodeImageSource;
            set => SetProperty(ref _qrCodeImageSource, value);
        }

        // Add these fields
        private System.Timers.Timer _qrCodeRefreshTimer;
        private const int QR_CODE_REFRESH_INTERVAL = 10000; // 30 seconds (Standard interval by steam) in milliseconds

        // Commands for login actions
        public ICommand TraditionalLoginCommand { get; }
        public ICommand SessionKeyLoginCommand { get; }
        public ICommand QrCodeLoginCommand { get; }
        public ICommand GenerateQrCodeCommand { get; }

        public enum LoginMethod
        {
            Traditional,
            SessionKey,
            QRCode
        }
        private LoginMethod _currentMethod;
        public LoginMethod CurrentMethod
        {
            get => _currentMethod;
            set
            {
                if (_currentMethod != value)
                {
                    SetProperty(ref _currentMethod, value);
                    // Explicitly reset the login status
                    LoginStatus = string.Empty;
                    OnPropertyChanged(nameof(LoginStatus));
                }
            }
        }


        public LoginViewModel(ISteamAuthService authService)
        {
            Title = "Login";
            _authService = authService;

            TraditionalLoginCommand = new Command(async () => await ExecuteTraditionalLoginCommand());
            SessionKeyLoginCommand = new Command(async () => await ExecuteSessionKeyLoginCommand());
            QrCodeLoginCommand = new Command(async () => await ExecuteQrCodeLoginCommand());
            GenerateQrCodeCommand = new Command(async () => await ExecuteGenerateQrCodeCommand());

            // Initialize the QR code refresh timer
            InitializeQRCodeRefreshTimer();
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
            {
                // If no session key is provided, open the browser to get one
                await OpenSessionKeyPage();
                return;
            }

            try
            {
                IsBusy = true;
                LoginStatus = "Logging in with session key...";

                bool success = await _authService.LoginWithSessionKeyAsync(SessionKey);

                if (success)
                {
                    string profileName = await _authService.GetPersonaNameAsync();
                    LoginStatus = $"Successfully logged in as {profileName}";

                    // Reset form fields
                    SessionKey = string.Empty;
                }
                else
                {
                    LoginStatus = "Login failed. Please check your session key.";
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

        private async Task OpenSessionKeyPage()
        {
            try
            {
                // Open the Steam session key page in the default browser
                await Browser.OpenAsync(new Uri("https://steamcommunity.com/chat/clientjstoken"), BrowserLaunchMode.SystemPreferred);
            }
            catch (Exception ex)
            {
                LoginStatus = $"Error opening browser: {ex.Message}";
            }
        }

        private async Task ExecuteQrCodeLoginCommand()
        {
            if (IsBusy)
                return;
            try
            {
                IsBusy = true;
                string token = await _authService.GenerateQRCodeTokenAsync();

                // Generate QR code image from the token
                if (!string.IsNullOrEmpty(token))
                {
                    GenerateQRCode(token);
                }
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

        private void GenerateQRCode(string content)
        {
            try
            {
                QRCodeGenerator qrGenerator = new QRCodeGenerator();
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
                PngByteQRCode qrCode = new PngByteQRCode(qrCodeData);
                byte[] qrCodeBytes = qrCode.GetGraphic(20);

                QrCodeImageSource = ImageSource.FromStream(() => new MemoryStream(qrCodeBytes));
                Debug.WriteLine("QR code image generated successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error generating QR code image: {ex.Message}");
            }
        }


        //DELETE THIS ????
        private async Task DisplayQRCode(string qrCodeUrl)
        {
            // For now, we'll just show the URL in a dialog
            // In a real app, you'd generate and display an actual QR code image
            await Application.Current.MainPage.DisplayAlert(
                "Scan QR Code",
                $"Scan this QR code with your Steam mobile app:\n\n{qrCodeUrl}",
                "OK");
        }

        private async Task ExecuteGenerateQrCodeCommand(bool isRefresh = false)
        {
            if (IsBusy)
                return;
            try
            {
                IsBusy = true;

                // Only change the status if it's not a refresh operation
                if (!isRefresh)
                {
                    LoginStatus = "Generating QR code...";
                }

                // Stop the timer if it's running
                if (_qrCodeRefreshTimer != null && _qrCodeRefreshTimer.Enabled)
                    _qrCodeRefreshTimer.Stop();

                // Generate QR code
                string token = await _authService.GenerateQRCodeTokenAsync();


                LoginStatus = "Logging in with QR code...";

                //OLD CODE????
                //// Wait for user to scan QR code
                //bool success = await _authService.LoginWithQRCodeAsync(QrCodeUrl);

                //if (success)
                //{
                //    string profileName = await _authService.GetPersonaNameAsync();
                //    LoginStatus = $"Successfully logged in as {profileName}";
                //}
                //else
                //{
                //    LoginStatus = "QR code login failed";
                //}

                if (string.IsNullOrEmpty(token))
                {
                    LoginStatus = "Failed to generate QR code";
                    return;
                }

                // Store the token for later use in login
                QrCodeUrl = token;

                // Generate QR code image from the token
                GenerateQRCode(token);

                LoginStatus = "Scan QR code with Steam mobile app";

                // Start the timer for auto-refresh
                _qrCodeRefreshTimer.Start();

                // Start polling for QR code scan in the background
                _ = PollForQRCodeScanAsync();
            }
            catch (Exception ex)
            {
                LoginStatus = $"Error: {ex.Message}";
                Debug.WriteLine($"QR code login error: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task PollForQRCodeScanAsync()
        {
            try
            {
                // Call LoginWithQRCodeAsync which will poll for the result
                bool success = await _authService.LoginWithQRCodeAsync(QrCodeUrl);

                // Stop the timer if login succeeds
                if (success)
                {
                    // Stop the refresh timer once we get a result
                    if (_qrCodeRefreshTimer != null && _qrCodeRefreshTimer.Enabled)
                        _qrCodeRefreshTimer.Stop();

                    string profileName = await _authService.GetPersonaNameAsync();
                    LoginStatus = $"Successfully logged in as {profileName}";
                }
            }
            catch (Exception ex)
            {
                // Catch any exceptions to prevent app from exiting
                Debug.WriteLine($"Error polling for QR code scan: {ex.Message}");
                //if (_currentMethod == LoginMethod.QRCode)
                //{
                //    LoginStatus = "Scan QR code with Steam mobile app";
                //}
            }
        }

        //initialize the timer
        private void InitializeQRCodeRefreshTimer()
        {
            _qrCodeRefreshTimer = new System.Timers.Timer(QR_CODE_REFRESH_INTERVAL);

            // Add try-catch around the timer callback
            _qrCodeRefreshTimer.Elapsed += async (sender, e) => {
                try
                {
                    await RefreshQRCodeAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error in timer callback: {ex.Message}");
                    // Make sure to update UI on the main thread
                    await MainThread.InvokeOnMainThreadAsync(() => {
                        if (_currentMethod == LoginMethod.QRCode)
                        {
                            LoginStatus = "Scan QR code with Steam mobile app";
                        }
                    });
                }
            };

            _qrCodeRefreshTimer.AutoReset = true;
        }

        // refresh the QR code
        private async Task RefreshQRCodeAsync()
        {
            try
            {
                // Execute on the UI thread
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    if (_currentMethod == LoginMethod.QRCode && !IsBusy)
                    {
                        Debug.WriteLine("Automatically refreshing QR code after 30 seconds");
                        // Update status to indicate refreshing
                        LoginStatus = "Refreshing QR code...";
                        await ExecuteGenerateQrCodeCommand(true);
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error refreshing QR code: {ex.Message}");
            }
        }

    }
}

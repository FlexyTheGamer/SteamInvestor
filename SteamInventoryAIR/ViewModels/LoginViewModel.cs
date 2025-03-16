using System.Windows.Input;
using System.Diagnostics;
using SteamInventoryAIR.Interfaces;

using Microsoft.Maui.ApplicationModel;

using QRCoder;
using System.IO;
using SteamInventoryAIR.Services;



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
        private const int QR_CODE_REFRESH_INTERVAL = 30000; // 30 seconds (Standard interval by steam) in milliseconds

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

            // IMPORTANT: This code loads debug credentials - REMOVE IN PRODUCTION
            LoadDebugCredentials();

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


                    // ADDED: Wait 2 seconds before navigating
                    Debug.WriteLine("Login successful, waiting 2 seconds before navigating to MainPage...");
                    await Task.Delay(2000);

                    // ADDED: Navigate to MainPage
                    // MODIFIED: Use proper Shell navigation
                    Debug.WriteLine("Navigating to MainPage");
                    await Shell.Current.GoToAsync("//main");
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

            //THERE ARE 3 nested try-catch blocks in this method that are potentially unnecessary.......
            try
            {
                Debug.WriteLine("=== PollForQRCodeScanAsync: Starting QR code polling ===");

                // Call LoginWithQRCodeAsync which will poll for the result
                Debug.WriteLine("Calling _authService.LoginWithQRCodeAsync");
                bool success = await _authService.LoginWithQRCodeAsync(QrCodeUrl);

                // Stop the timer if login succeeds
                if (success)
                {
                    if (_qrCodeRefreshTimer != null && _qrCodeRefreshTimer.Enabled)
                    {
                        Debug.WriteLine("Stopping QR code refresh timer after successful login");
                        _qrCodeRefreshTimer.Stop();
                    }

                    string profileName = await _authService.GetPersonaNameAsync();
                    LoginStatus = $"Successfully logged in as {profileName}";
                }
                else
                {
                    // Login failed but don't update status if we're refreshing
                    if (LoginStatus != "Refreshing QR code...")
                    {
                        LoginStatus = "QR code login failed. Please try scanning again.";
                    }
                }

                /////////Trying to simplify the code back to the original when the qr code worked...
                //// Use Task.Run to run the polling in a background thread
                //await Task.Run(async () => {
                //    try
                //    {
                //        bool success = await _authService.LoginWithQRCodeAsync(QrCodeUrl);

                //        // Handle result on UI thread
                //        await MainThread.InvokeOnMainThreadAsync(() => {
                //            if (success)
                //            {
                //                if (_qrCodeRefreshTimer != null && _qrCodeRefreshTimer.Enabled)
                //                    _qrCodeRefreshTimer.Stop();

                //                // Get username in a separate try block
                //                try
                //                {
                //                    var profileName = _authService.GetPersonaNameAsync().Result;
                //                    LoginStatus = $"Successfully logged in as {profileName}";
                //                }
                //                catch
                //                {
                //                    LoginStatus = "Successfully logged in";
                //                }
                //            }
                //        });
                //    }
                //    catch (Exception ex)
                //    {
                //        Debug.WriteLine($"Background polling error: {ex.Message}");
                //    }
                //});
            }
            catch (Exception ex)
            {
                // Catch any exceptions to prevent app from exiting
                Debug.WriteLine($"=== ERROR in PollForQRCodeScanAsync: {ex.Message} ===");
                Debug.WriteLine($"Exception type: {ex.GetType().Name}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        //initialize the timer
        private void InitializeQRCodeRefreshTimer()
        {
            Debug.WriteLine("=== InitializeQRCodeRefreshTimer ===");
            _qrCodeRefreshTimer = new System.Timers.Timer(QR_CODE_REFRESH_INTERVAL);
            Debug.WriteLine($"Created QR code refresh timer with interval {QR_CODE_REFRESH_INTERVAL}ms");

            // Add try-catch around the timer callback
            _qrCodeRefreshTimer.Elapsed += async (sender, e) => {
                try
                {
                    Debug.WriteLine("QR code refresh timer elapsed - triggering refresh");
                    await RefreshQRCodeAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"=== ERROR in timer callback: {ex.Message} ===");
                    Debug.WriteLine($"Exception type: {ex.GetType().Name}");
                    Debug.WriteLine($"Stack trace: {ex.StackTrace}");

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
            Debug.WriteLine("QR code timer initialized with AutoReset = true");
        }

        // refresh the QR code
        private async Task RefreshQRCodeAsync()
        {
            try
            {
                Debug.WriteLine("=== RefreshQRCodeAsync: QR code refresh timer triggered ===");

                ///////////Trying to simplfy things as it was before the qr code stopped working...
                //// Execute on the UI thread
                //await MainThread.InvokeOnMainThreadAsync(async () =>
                //{
                //    if (_currentMethod == LoginMethod.QRCode && !IsBusy)
                //    {
                //        Debug.WriteLine("Automatically refreshing QR code after timeout");

                //        // Update status to indicate refreshing
                //        LoginStatus = "Refreshing QR code...";
                //        Debug.WriteLine("Set LoginStatus to 'Refreshing QR code...'");

                //        // Generate a new QR code (this will start a new authentication session)
                //        await ExecuteGenerateQrCodeCommand(true);

                //        // Previous polling operation will fail, but the exception is handled
                //    }
                //    else
                //    {
                //        Debug.WriteLine($"Skipping QR refresh: CurrentMethod={_currentMethod}, IsBusy={IsBusy}");
                //    }
                //});


                Debug.WriteLine("QR code refresh timer triggered");

                // Only refresh if we're still on the QR code tab and not busy
                if (_currentMethod == LoginMethod.QRCode && !IsBusy)
                {
                    Debug.WriteLine("Automatically refreshing QR code after timeout");
                    Debug.WriteLine("Refreshing QR code");
                    LoginStatus = "Refreshing QR code...";
                    Debug.WriteLine("Set LoginStatus to 'Refreshing QR code...'");

                    // Just generate a new QR code
                    await ExecuteGenerateQrCodeCommand(true);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"=== ERROR in RefreshQRCodeAsync: {ex.Message} ===");
                Debug.WriteLine($"Exception type: {ex.GetType().Name}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        public void LoadDebugCredentials()
        {
            // IMPORTANT: This code is for debugging only and should be removed in production builds
            Debug.WriteLine("Loading debug credentials from environment variables");

            // Load credentials from environment variables
            string username = EnvironmentService.GetVariable("STEAM_DEBUG_USERNAME", "");
            string password = EnvironmentService.GetVariable("STEAM_DEBUG_PASSWORD", "");
            string sessionKey = EnvironmentService.GetVariable("STEAM_DEBUG_SESSION_KEY", "");

            // Log what values were loaded
            Debug.WriteLine($"Loaded username: {(string.IsNullOrEmpty(username) ? "empty" : "not empty")}");
            Debug.WriteLine($"Loaded password: {(string.IsNullOrEmpty(password) ? "empty" : "not empty")}");

            // Set properties and force notification
            Username = username;
            OnPropertyChanged(nameof(Username));

            Password = password;
            OnPropertyChanged(nameof(Password));

            SessionKey = sessionKey;
            OnPropertyChanged(nameof(SessionKey));

            if (!string.IsNullOrEmpty(Username) || !string.IsNullOrEmpty(Password))
            {
                Debug.WriteLine("Debug credentials loaded successfully");
            }
        }

    }
}

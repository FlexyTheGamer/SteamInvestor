// ViewModels/LoginViewModel.cs
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.ApplicationModel;
using QRCoder;
using SteamInventoryAIR.Interfaces;

namespace SteamInventoryAIR.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        private readonly ISteamAuthService _authService;

        // Properties
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

        private string _sessionKey;
        public string SessionKey
        {
            get => _sessionKey;
            set => SetProperty(ref _sessionKey, value);
        }

        private string _qrCodeUrl;
        public string QrCodeUrl
        {
            get => _qrCodeUrl;
            set => SetProperty(ref _qrCodeUrl, value);
        }

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

        // Login method enum
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
                    LoginStatus = string.Empty;
                }
            }
        }

        // QR code refresh timer
        private System.Timers.Timer _qrCodeRefreshTimer;
        private const int QR_CODE_REFRESH_INTERVAL = 30000; // 30 seconds

        // Commands
        public ICommand TraditionalLoginCommand { get; }
        public ICommand SessionKeyLoginCommand { get; }
        public ICommand GenerateQrCodeCommand { get; }
        public ICommand QrCodeLoginCommand { get; }
        public ICommand OpenSessionKeyPageCommand { get; }

        public LoginViewModel(ISteamAuthService authService)
        {
            Title = "Login";
            _authService = authService;

            // Initialize commands
            TraditionalLoginCommand = new Command(async () => await LoginWithCredentialsAsync());
            SessionKeyLoginCommand = new Command(async () => await LoginWithSessionKeyAsync());
            GenerateQrCodeCommand = new Command(async () => await GenerateQrCodeAsync());
            QrCodeLoginCommand = new Command(async () => await LoginWithQrCodeAsync());
            OpenSessionKeyPageCommand = new Command(async () => await OpenSessionKeyPage());

            // Initialize QR code refresh timer
            InitializeQRCodeRefreshTimer();
        }

        private async Task LoginWithCredentialsAsync()
        {
            if (IsBusy || string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
                return;

            try
            {
                IsBusy = true;
                LoginStatus = "Logging in...";

                bool success = await _authService.LoginWithCredentialsAsync(Username, Password, AuthCode);

                if (success)
                {
                    string profileName = await _authService.GetPersonaNameAsync();
                    ProfileName = profileName;
                    LoginStatus = $"Successfully logged in as {profileName}";

                    // Clear form fields
                    Username = string.Empty;
                    Password = string.Empty;
                    AuthCode = string.Empty;

                    // TODO: Navigate to main page
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

        private async Task LoginWithSessionKeyAsync()
        {
            if (IsBusy)
                return;

            if (string.IsNullOrEmpty(SessionKey))
            {
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
                    ProfileName = profileName;
                    LoginStatus = $"Successfully logged in as {profileName}";

                    // Clear form field
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

        private async Task GenerateQrCodeAsync(bool isRefresh = false)
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;

                if (!isRefresh)
                {
                    LoginStatus = "Generating QR code...";
                }

                // Stop timer if running
                if (_qrCodeRefreshTimer != null && _qrCodeRefreshTimer.Enabled)
                {
                    _qrCodeRefreshTimer.Stop();
                }

                // Generate QR code
                string token = await _authService.GenerateQRCodeTokenAsync();

                if (string.IsNullOrEmpty(token))
                {
                    LoginStatus = "Failed to generate QR code";
                    return;
                }

                // Store token and generate image
                QrCodeUrl = token;
                GenerateQRCodeImage(token);

                LoginStatus = "Scan QR code with Steam mobile app";

                // Start timer for auto-refresh
                _qrCodeRefreshTimer.Start();

                // Start polling in background
                _ = PollForQrCodeScanAsync();
            }
            catch (Exception ex)
            {
                LoginStatus = $"Error: {ex.Message}";
                Debug.WriteLine($"QR code generation error: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoginWithQrCodeAsync()
        {
            if (IsBusy || string.IsNullOrEmpty(QrCodeUrl))
                return;

            try
            {
                IsBusy = true;
                LoginStatus = "Authenticating with QR code...";

                bool success = await _authService.LoginWithQRCodeAsync(QrCodeUrl);

                if (success)
                {
                    // Stop the timer
                    if (_qrCodeRefreshTimer != null && _qrCodeRefreshTimer.Enabled)
                    {
                        _qrCodeRefreshTimer.Stop();
                    }

                    string profileName = await _authService.GetPersonaNameAsync();
                    ProfileName = profileName;
                    LoginStatus = $"Successfully logged in as {profileName}";
                }
                else
                {
                    LoginStatus = "QR code login failed. Please try again.";
                }
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

        private async Task OpenSessionKeyPage()
        {
            try
            {
                await Browser.OpenAsync(new Uri("https://steamcommunity.com/chat/clientjstoken"), BrowserLaunchMode.SystemPreferred);
            }
            catch (Exception ex)
            {
                LoginStatus = $"Error opening browser: {ex.Message}";
            }
        }

        private async Task PollForQrCodeScanAsync()
        {
            try
            {
                bool success = await _authService.LoginWithQRCodeAsync(QrCodeUrl);

                if (success)
                {
                    // Stop the timer
                    if (_qrCodeRefreshTimer != null && _qrCodeRefreshTimer.Enabled)
                    {
                        _qrCodeRefreshTimer.Stop();
                    }

                    string profileName = await _authService.GetPersonaNameAsync();
                    ProfileName = profileName;
                    LoginStatus = $"Successfully logged in as {profileName}";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"QR code polling error: {ex.Message}");
            }
        }

        private void GenerateQRCodeImage(string content)
        {
            try
            {
                using (var qrGenerator = new QRCodeGenerator())
                {
                    var qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
                    var qrCode = new PngByteQRCode(qrCodeData);
                    byte[] qrCodeBytes = qrCode.GetGraphic(20);

                    QrCodeImageSource = ImageSource.FromStream(() => new MemoryStream(qrCodeBytes));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error generating QR code image: {ex.Message}");
            }
        }

        private void InitializeQRCodeRefreshTimer()
        {
            _qrCodeRefreshTimer = new System.Timers.Timer(QR_CODE_REFRESH_INTERVAL);

            _qrCodeRefreshTimer.Elapsed += async (sender, e) => {
                try
                {
                    await MainThread.InvokeOnMainThreadAsync(async () => {
                        if (CurrentMethod == LoginMethod.QRCode && !IsBusy)
                        {
                            LoginStatus = "Refreshing QR code...";
                            await GenerateQrCodeAsync(true);
                        }
                    });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"QR code refresh error: {ex.Message}");
                }
            };

            _qrCodeRefreshTimer.AutoReset = true;
        }

        private async Task RefreshQrCodeAsync()
        {
            if (CurrentMethod != LoginMethod.QRCode || IsBusy)
                return;

            try
            {
                LoginStatus = "Refreshing QR code...";
                await GenerateQrCodeAsync(true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"QR code refresh error: {ex.Message}");
            }
        }
    }
}

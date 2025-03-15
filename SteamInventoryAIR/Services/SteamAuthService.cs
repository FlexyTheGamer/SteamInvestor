// Services/SteamAuthService.cs
using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using SteamInventoryAIR.Interfaces;
using SteamInventoryAIR.Models;
using SteamKit2;
using SteamKit2.Authentication;

namespace SteamInventoryAIR.Services
{
    public class SteamAuthService : ISteamAuthService
    {
        private readonly SteamAuthModel _model;
        private TaskCompletionSource<bool>? _loginTcs;
        private TaskCompletionSource<string>? _personaNameTcs;
        private string? _currentUsername;
        private string? _currentPassword;
        private string? _authCode;
        private string? _twoFactorCode;
        private string? _qrCodeUrl;
        private string? _sessionKey;
        private QrAuthSession? _authSession;

        public SteamAuthService()
        {
            _model = new SteamAuthModel();

            // Register for model events
            _model.ConnectionStatusChanged += OnConnectionStatusChanged;
            _model.LoginStatusChanged += OnLoginStatusChanged;
            _model.PersonaStateChanged += OnPersonaStateChanged;
        }

        public async Task<bool> LoginWithCredentialsAsync(string username, string password, string? authCode = null)
        {
            try
            {
                _loginTcs = new TaskCompletionSource<bool>();
                _currentUsername = username;
                _currentPassword = password;
                _authCode = authCode;
                _twoFactorCode = null;

                // Connect to Steam
                _model.Connect();

                // Wait for the login result (will be set by event handlers)
                return await _loginTcs.Task;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LoginWithCredentialsAsync error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> LoginWithSessionKeyAsync(string sessionKey)
        {
            try
            {
                _loginTcs = new TaskCompletionSource<bool>();

                string? username = null;

                // Check if the input is a JSON string
                if (sessionKey.Trim().StartsWith("{"))
                {
                    var jsonResponse = JsonSerializer.Deserialize<SessionKeyResponse>(sessionKey);

                    if (jsonResponse == null || !jsonResponse.logged_in)
                    {
                        Debug.WriteLine("The provided session data indicates you're not logged in to Steam");
                        return false;
                    }

                    username = jsonResponse.account_name;
                    sessionKey = jsonResponse.token;
                }

                _currentUsername = username;

                // Connect to Steam
                _model.Connect();

                // The rest will be handled in the connection event
                // Store the session key for use when connected
                _sessionKey = sessionKey;

                return await _loginTcs.Task;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LoginWithSessionKeyAsync error: {ex.Message}");
                return false;
            }
        }

        public async Task<string?> GenerateQRCodeTokenAsync()
        {
            try
            {
                // Connect to Steam if not already connected
                if (!_model.IsLoggedIn())
                {
                    var connectionTcs = new TaskCompletionSource<bool>();

                    // Register temporary event handler for connection status
                    void ConnectionHandler(bool isConnected)
                    {
                        if (isConnected)
                        {
                            connectionTcs.TrySetResult(true);
                        }
                    }

                    // Add temporary handler
                    _model.ConnectionStatusChanged += ConnectionHandler;

                    // Initiate connection
                    _model.Connect();

                    // Wait for connection with a timeout
                    var timeoutTask = Task.Delay(TimeSpan.FromSeconds(10));
                    var completedTask = await Task.WhenAny(connectionTcs.Task, timeoutTask);

                    // Remove temporary handler
                    _model.ConnectionStatusChanged -= ConnectionHandler;

                    // Check if we timed out
                    if (completedTask == timeoutTask)
                    {
                        Debug.WriteLine("Connection to Steam timed out");
                        return null;
                    }
                }

                // Begin QR authentication session
                _authSession = await _model.BeginQRAuthAsync();

                // Get challenge URL from auth session
                if (_authSession != null)
                {
                    _qrCodeUrl = _authSession.ChallengeURL;
                    return _qrCodeUrl;
                }
                else
                {
                    Debug.WriteLine("Failed to create QR authentication session");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GenerateQRCodeTokenAsync error: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> LoginWithQRCodeAsync(string qrToken)
        {
            try
            {
                _loginTcs = new TaskCompletionSource<bool>();

                if (_authSession == null)
                {
                    Debug.WriteLine("No active QR authentication session");
                    return false;
                }

                // Poll for QR code authentication result
                var pollResponse = await _authSession.PollingWaitForResultAsync();

                // Log on with the access token
                _model.LogOnWithToken(pollResponse.AccountName, pollResponse.RefreshToken);

                return await _loginTcs.Task;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LoginWithQRCodeAsync error: {ex.Message}");
                return false;
            }
        }

        public Task<bool> IsLoggedInAsync()
        {
            return Task.FromResult(_model.IsLoggedIn());
        }

        public Task<bool> LogoutAsync()
        {
            if (_model.IsLoggedIn())
            {
                _model.Disconnect();
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        public async Task<string> GetPersonaNameAsync()
        {
            // If we already have a persona name, return it
            string personaName = _model.GetPersonaName();
            if (!string.IsNullOrEmpty(personaName))
            {
                return personaName;
            }

            // Otherwise, request it and wait for the result
            if (_model.IsLoggedIn())
            {
                _personaNameTcs = new TaskCompletionSource<string>();
                _model.RequestPersonaInfo();

                // Wait with a timeout
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(3));
                var completedTask = await Task.WhenAny(_personaNameTcs.Task, timeoutTask);

                if (completedTask == _personaNameTcs.Task)
                {
                    return await _personaNameTcs.Task;
                }
            }

            return "Unknown User";
        }

        public async Task<bool> SubmitTwoFactorCodeAsync(string twoFactorCode)
        {
            try
            {
                _loginTcs = new TaskCompletionSource<bool>();
                _twoFactorCode = twoFactorCode;

                // Reconnect with the two-factor code
                _model.Connect();

                return await _loginTcs.Task;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SubmitTwoFactorCodeAsync error: {ex.Message}");
                return false;
            }
        }

        // Event handlers
        private async void OnConnectionStatusChanged(bool isConnected)
        {
            if (isConnected)
            {
                try
                {
                    // We are now connected to Steam, proceed with authentication
                    if (!string.IsNullOrEmpty(_sessionKey))
                    {
                        // Session key login
                        _model.LogOnWithToken(_currentUsername, _sessionKey);
                        _sessionKey = null; // Clear the session key after use
                    }
                    else
                    {
                        // Credential login
                        if (_currentPassword != null)
                        {
                            var authSession = await _model.BeginCredentialsAuthAsync(
                                _currentUsername,
                                _currentPassword,
                                _authCode,
                                _twoFactorCode);

                            var pollResponse = await authSession.PollingWaitForResultAsync();

                            _model.LogOnWithToken(
                                pollResponse.AccountName,
                                pollResponse.RefreshToken);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Authentication error: {ex.Message}");
                    _loginTcs?.TrySetResult(false);
                }
            }
        }

        private void OnLoginStatusChanged(bool isLoggedIn, EResult result)
        {
            if (isLoggedIn)
            {
                _loginTcs?.TrySetResult(true);
            }
            else
            {
                string errorMessage = $"Login failed: {result}";

                if (result == EResult.AccountLogonDenied)
                {
                    errorMessage = "This account is protected by Steam Guard email.";
                }
                else if (result == EResult.AccountLoginDeniedNeedTwoFactor)
                {
                    errorMessage = "This account is protected by Steam Guard Mobile Authenticator.";
                }
                else if (result == EResult.InvalidPassword)
                {
                    errorMessage = "Invalid password provided.";
                }

                Debug.WriteLine(errorMessage);
                _loginTcs?.TrySetResult(false);
            }
        }

        private void OnPersonaStateChanged(string personaName)
        {
            _personaNameTcs?.TrySetResult(personaName);
        }

        // Helper classes
        private class SessionKeyResponse
        {
            public bool logged_in { get; set; }
            public string steamid { get; set; }
            public int accountid { get; set; }
            public string account_name { get; set; }
            public string token { get; set; }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SteamInventoryAIR.Interfaces;
using SteamKit2;
using SteamKit2.Authentication;
using static SteamKit2.Internal.CMsgRemoteClientBroadcastStatus;

namespace SteamInventoryAIR.Services
{
    public  class SteamAuthService : ISteamAuthService
    {

        private SteamClient _steamClient;

        private SteamUser _steamUser;
        private SteamFriends _steamFriends;
        private CallbackManager _manager;
        private bool _isRunning;
        private string _authCode;
        private string _twoFactorCode;
        private bool _isLoggedIn;
        private SteamID _steamId;
        private string _personaName;

        // TaskCompletionSource for login process
        private TaskCompletionSource<bool> _loginTcs;

        // Store current login credentials
        private string _currentUsername;
        private string _currentPassword;

        private string _previousGuardData;

        public SteamAuthService()
        {
            InitializeSteamClient();
        }

        private void InitializeSteamClient()
        {
            _steamClient = new SteamClient();
            _steamUser = _steamClient.GetHandler<SteamUser>();
            _steamFriends = _steamClient.GetHandler<SteamFriends>();
            _manager = new CallbackManager(_steamClient);

            // Register callbacks
            _manager.Subscribe<SteamClient.ConnectedCallback>(OnConnected);
            _manager.Subscribe<SteamClient.DisconnectedCallback>(OnDisconnected);
            _manager.Subscribe<SteamUser.LoggedOnCallback>(OnLoggedOn);
            _manager.Subscribe<SteamUser.LoggedOffCallback>(OnLoggedOff);

            _manager.Subscribe<SteamFriends.PersonaStateCallback>(OnPersonaState);


            // Start the client thread
            _isRunning = true;
            Task.Run(() => RunCallbacks());
        }

        private void RunCallbacks()
        {
            while (_isRunning)
            {
                _manager.RunWaitCallbacks(TimeSpan.FromSeconds(1));
            }
        }

        public async Task<bool> LoginWithCredentialsAsync(string username, string password, string? authCode = null)
        {
            _authCode = authCode;
            _twoFactorCode = null; // Reset two-factor code
            SetCredentials(username, password);
            _loginTcs = new TaskCompletionSource<bool>();

            // Connect to Steam
            _steamClient.Connect();

            return await _loginTcs.Task;
        }

        public async Task<bool> LoginWithSessionKeyAsync(string sessionKey)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> LoginWithQRCodeAsync(string qrToken)
        {
            throw new NotImplementedException();
        }

        public async Task<string> GenerateQRCodeTokenAsync()
        {
            //throw new NotImplementedException();
            return "placeholder-qr-token";
        }

        public async Task<bool> IsLoggedInAsync()
        {
            return _isLoggedIn;
        }

        public async Task LogoutAsync()
        {
            if (_isLoggedIn)
            {
                _steamUser.LogOff();
                _isLoggedIn = false;
            }
        }

        public async Task<string> GetPersonaNameAsync()
        {
            // If persona name is empty but we're logged in, try to get it again
            if (string.IsNullOrEmpty(_personaName) && _isLoggedIn && _steamFriends != null)
            {
                try
                {
                    _personaName = _steamFriends.GetPersonaName();
                    Console.WriteLine($"Retrieved persona name in GetPersonaNameAsync: {_personaName}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error getting persona name: {ex.Message}");
                }
            }

            return _personaName ?? "Unknown User";
        }

        private class CustomAuthenticator : IAuthenticator
        {
            private readonly string _authCode;
            private readonly string _twoFactorCode;

            public CustomAuthenticator(string authCode, string twoFactorCode)
            {
                _authCode = authCode;
                _twoFactorCode = twoFactorCode;
            }

            public Task<string> GetDeviceCodeAsync(bool previousCodeWasIncorrect)
            {
                return Task.FromResult(_authCode);
            }

            public Task<string> GetEmailCodeAsync(string email, bool previousCodeWasIncorrect)
            {
                return Task.FromResult(_authCode);
            }

            public Task<string> GetTwoFactorCodeAsync(bool previousCodeWasIncorrect)
            {
                return Task.FromResult(_twoFactorCode);
            }
            public Task<bool> AcceptDeviceConfirmationAsync()
            {
                // For most authentication flows, returning true is sufficient
                // This indicates that the device confirmation is accepted automatically
                return Task.FromResult(true);
            }
        }


        //Callback handlers
        private async void OnConnected(SteamClient.ConnectedCallback callback)
        {
            Console.WriteLine("Connected to Steam");

            var shouldRememberPassword = false;

            try
            {
                // Begin authenticating via credentials
                var authSession = await _steamClient.Authentication.BeginAuthSessionViaCredentialsAsync(new AuthSessionDetails
                {
                    Username = _currentUsername,  
                    Password = _currentPassword,
                    IsPersistentSession = shouldRememberPassword,
                    GuardData = _previousGuardData, // You can store and use previous guard data if needed
                    Authenticator = new CustomAuthenticator(_authCode, _twoFactorCode),         //Umjesto UserConsoleAuthenticator() koristimo CustomAuthenticator(2arg) 
                });

                // Starting polling Steam for authentication response
                var pollResponse = await authSession.PollingWaitForResultAsync();


                if (pollResponse.NewGuardData != null)
                {
                    // When using certain two factor methods (such as email 2fa), guard data may be provided by Steam
                    // for use in future authentication sessions to avoid triggering 2FA again (this works similarly to the old sentry file system).
                    // Do note that this guard data is also a JWT token and has an expiration date.
                    _previousGuardData = pollResponse.NewGuardData;
                }

                // Logon to Steam with the access token we have received
                _steamUser.LogOn(new SteamUser.LogOnDetails
                {
                    Username = pollResponse.AccountName,
                    AccessToken = pollResponse.RefreshToken,
                    ShouldRememberPassword = shouldRememberPassword,
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Authentication error: {ex.Message}");
                _loginTcs.SetResult(false);
            }
        }


        private void OnDisconnected(SteamClient.DisconnectedCallback callback)
        {
            Console.WriteLine("Disconnected from Steam");

            _isLoggedIn = false;

            // If we were logged in, reconnect
            if (_isLoggedIn)
            {
                Console.WriteLine("Reconnecting to Steam...");
                _steamClient.Connect();
            }
            else if (_loginTcs != null && !_loginTcs.Task.IsCompleted)
            {
                _loginTcs.SetResult(false);
            }
        }

        private void OnLoggedOn(SteamUser.LoggedOnCallback callback)
        {
            if (callback.Result != EResult.OK)
            {
                Console.WriteLine("Unable to log in to Steam: {0}", callback.Result);

                if (callback.Result == EResult.AccountLogonDenied)
                {
                    // Steam Guard is enabled and we need an auth code
                    Console.WriteLine("This account is protected by Steam Guard. Enter the auth code sent to the associated email.");
                    //_loginTcs.SetResult(false);
                }
                else if (callback.Result == EResult.AccountLoginDeniedNeedTwoFactor)
                {
                    // Two-factor authentication required
                    Console.WriteLine("This account is protected by Steam Guard Mobile Authenticator. You need to provide the two-factor code from your mobile app.");
                    //_loginTcs.SetResult(false);
                }
                else if (callback.Result == EResult.InvalidPassword)
                {
                    Console.WriteLine("Invalid password provided.");
                    //_loginTcs.SetResult(false);
                }

                _loginTcs.SetResult(false);
                return;
            }

            Console.WriteLine("Successfully logged in to Steam");
            _isLoggedIn = true;
            _steamId = callback.ClientSteamID;

            // Get persona name after successful login
            // Important: We need to wait a moment for Steam to initialize the friends list
            _steamFriends.SetPersonaState(EPersonaState.Online);

            _loginTcs.SetResult(true);
        }

        private void OnLoggedOff(SteamUser.LoggedOffCallback callback)
        {
            Console.WriteLine("Logged off of Steam: {0}", callback.Result);
            _isLoggedIn = false;
        }

        private void OnPersonaState(SteamFriends.PersonaStateCallback callback)
        {
            // This callback is triggered when the Steam network sends information about a user's status.
            // In this case, we're interested in our own status.
            if (callback.FriendID == _steamId)
            {
                _personaName = callback.Name;
                Console.WriteLine($"Updated persona name: {_personaName}");
            }
        }



        // Helper method to set credentials - ????????
        private void SetCredentials(string username, string password)
        {
            _currentUsername = username;
            _currentPassword = password;
        }

        // Add method to handle two-factor authentication
        public async Task<bool> SubmitTwoFactorCodeAsync(string twoFactorCode)
        {
            _twoFactorCode = twoFactorCode;
            _loginTcs = new TaskCompletionSource<bool>();

            // Reconnect with the two-factor code
            _steamClient.Connect();

            return await _loginTcs.Task;
        }
    }
}

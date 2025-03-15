using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SteamInventoryAIR.Interfaces;
using SteamKit2;
using SteamKit2.Authentication;
using SteamKit2.Internal;
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

        // TaskCompletionSource for persona name retrieval
        private TaskCompletionSource<string> _personaNameTcs;


        // Store current login credentials
        private string _currentUsername;
        private string _currentPassword;

        private string _previousGuardData;


        //Login over session key variables
        private string _sessionKey;
        private bool _isSessionKeyLogin;


        private bool _isQrCodeLogin;
        private AuthPollResult _qrPollResult;
        private AuthSession _qrAuthSession;

        private TaskCompletionSource<string> _qrCodeTcs;



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

        public async Task<bool> LoginWithSessionKeyAsync_OLD(string sessionKey)
        {
            _loginTcs = new TaskCompletionSource<bool>();

            _personaNameTcs = new TaskCompletionSource<string>();

            try
            {
                // Reset persona name
                _personaName = null;


                // Check if the input is a JSON string
                if (sessionKey.Trim().StartsWith("{"))
                {
                    // Parse the JSON to extract the token
                    var jsonResponse = System.Text.Json.JsonSerializer.Deserialize<SessionKeyResponse>(sessionKey);

                    if (!jsonResponse.logged_in)
                    {
                        Debug.WriteLine("The provided session data indicates you're not logged in to Steam");
                        _loginTcs.SetResult(false);
                        return await _loginTcs.Task;
                    }

                    // Extract just the token part
                    sessionKey = jsonResponse.token;

                    // Store the Steam ID for later use
                    _steamId = new SteamID(Convert.ToUInt64(jsonResponse.steamid));
                    Debug.WriteLine($"Set Steam ID to: {_steamId}");

                }

                // Connect to Steam using the session key
                _steamClient.Connect();

                // The session key login will be handled in OnConnected
                _sessionKey = sessionKey;
                _isSessionKeyLogin = true;

                return await _loginTcs.Task;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error logging in with session key: {ex.Message}");
                _loginTcs.SetResult(false);
                return false;
            }
        }
        //old version above, new version below
        public async Task<bool> LoginWithSessionKeyAsync(string sessionKey)
        {
            _loginTcs = new TaskCompletionSource<bool>();
            _personaName = null; // Reset persona name

            try
            {
                string username = null;

                // Check if the input is a JSON string
                if (sessionKey.Trim().StartsWith("{"))
                {
                    // Parse the JSON to extract the token
                    var jsonResponse = System.Text.Json.JsonSerializer.Deserialize<SessionKeyResponse>(sessionKey);

                    if (!jsonResponse.logged_in)
                    {
                        Debug.WriteLine("The provided session data indicates you're not logged in to Steam");
                        _loginTcs.SetResult(false);
                        return await _loginTcs.Task;
                    }

                    // Extract the username
                    username = jsonResponse.account_name;
                    Debug.WriteLine($"Extracted username: {username}");

                    // Extract just the token part
                    sessionKey = jsonResponse.token;

                    // Store the Steam ID for later use
                    ulong steamIdValue;
                    if (ulong.TryParse(jsonResponse.steamid, out steamIdValue))
                    {
                        _steamId = new SteamID(steamIdValue);
                        Debug.WriteLine($"Set Steam ID to: {_steamId}");
                    }
                    else
                    {
                        Debug.WriteLine($"Failed to parse Steam ID: {jsonResponse.steamid}");
                    }
                }

                // Store the username for use in OnConnected
                _currentUsername = username;

                // Connect to Steam using the session key
                _steamClient.Connect();

                // The session key login will be handled in OnConnected
                _sessionKey = sessionKey;
                _isSessionKeyLogin = true;

                return await _loginTcs.Task;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error logging in with session key: {ex.Message}");
                _loginTcs.SetResult(false);
                return false;
            }
        }


        // Add this class to parse the JSON response
        private class SessionKeyResponse
        {
            public bool logged_in { get; set; }
            public string steamid { get; set; }
            public int accountid { get; set; }
            public string account_name { get; set; }
            public string token { get; set; }
        }


        public async Task<bool> LoginWithQRCodeAsync(string qrToken)
        {
            try
            {
                if (_qrAuthSession == null)
                {
                    Debug.WriteLine("No active QR auth session");
                    return false;
                }

                Debug.WriteLine("Starting to poll for QR code authentication result");
                _loginTcs = new TaskCompletionSource<bool>();

                //Removed in proccess of removing the cacnelation token
                ////??Nested Try Catch block - this is not really good practice???, but it's a good way to handle exceptions in this case???
                //try
                //{
                //    // Start polling for the authentication result with cancellation token
                //    var pollResponse = await _qrAuthSession.PollingWaitForResultAsync(_qrPollingCts.Token);
                //    Debug.WriteLine($"Received poll response: Account name = {pollResponse.AccountName}");
                //    Debug.WriteLine($"RefreshToken length: {pollResponse.RefreshToken?.Length ?? 0}");
                //    Debug.WriteLine($"AccessToken length: {pollResponse.AccessToken?.Length ?? 0}");

                //    // Log on with the access token we received
                //    Debug.WriteLine($"Attempting login with Username: {pollResponse.AccountName}");

                //    _steamUser.LogOn(new SteamUser.LogOnDetails
                //    {
                //        Username = pollResponse.AccountName,
                //        AccessToken = pollResponse.AccessToken,
                //        ShouldRememberPassword = false
                //    });

                //    // Wait for the login result
                //    return await _loginTcs.Task;
                //}
                //catch (OperationCanceledException)
                //{
                //    Debug.WriteLine("QR code polling was canceled - likely due to refresh");
                //    return false; // Return false instead of letting the exception propagate
                //}



                // Start polling for the authentication result without cancellation token
                var pollResponse = await _qrAuthSession.PollingWaitForResultAsync();
                Debug.WriteLine($"Received poll response: Account name = {pollResponse.AccountName}");
                Debug.WriteLine($"RefreshToken length: {pollResponse.RefreshToken?.Length ?? 0}");
                Debug.WriteLine($"AccessToken length: {pollResponse.AccessToken?.Length ?? 0}");

                // Log on with the access token we received
                Debug.WriteLine($"Attempting login with Username: {pollResponse.AccountName}");

                // Log on with the access token we received
                _steamUser.LogOn(new SteamUser.LogOnDetails
                {
                    Username = pollResponse.AccountName,
                    //AccessToken = pollResponse.RefreshToken,
                    AccessToken = pollResponse.AccessToken,
                    ShouldRememberPassword = false
                });

                // Wait for the login result
                return await _loginTcs.Task;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error logging in with QR code: {ex.Message}");
                Debug.WriteLine($"Exception details: {ex}");
                if (_loginTcs != null && !_loginTcs.Task.IsCompleted)
                    _loginTcs.SetResult(false);
                return false;
            }
        }

        public async Task<string> GenerateQRCodeTokenAsync()
        {
            try
            {

                _loginTcs = new TaskCompletionSource<bool>();

                // Create a new TaskCompletionSource for the QR code URL
                _qrCodeTcs = new TaskCompletionSource<string>();

                Debug.WriteLine("Initializing QR code generation");

                // Set a flag to indicate we're doing QR code authentication
                _isQrCodeLogin = true;

                // Connect to Steam
                if (_steamClient.IsConnected)
                {
                    Debug.WriteLine("Already connected to Steam, proceeding with QR code generation");
                }
                else
                {
                    Debug.WriteLine("Connecting to Steam for QR code generation");
                    _steamClient.Connect();
                }

                ////-----------------old code - removed becuase QR code generation happens before connecting to Steam
                //// Set a flag to indicate we're doing QR code authentication
                //_isQrCodeLogin = true;

                //// Create a new authentication session for QR login
                //var authSession = await _steamClient.Authentication.BeginAuthSessionViaQRAsync(new AuthSessionDetails
                //{
                //    PlatformType = EAuthTokenPlatformType.k_EAuthTokenPlatformType_MobileApp,
                //    ClientOSType = EOSType.Android9,
                //    Authenticator = new CustomAuthenticator(null, null)
                //});

                //// Get the QR code challenge URL
                //string qrCodeUrl = authSession.ChallengeURL;

                //// Store the auth session for later use
                //_qrAuthSession = authSession;

                //Debug.WriteLine($"Generated QR code URL: {qrCodeUrl}");

                return await _qrCodeTcs.Task;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error generating QR code: {ex.Message}");
                return null;
            }
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

        public async Task<string> GetPersonaNameAsync_OLD()
        {
            // If persona name is empty but we're logged in, try to get it again
            if (string.IsNullOrEmpty(_personaName) && _isLoggedIn && _steamFriends != null)
            {
                try
                {
                    _personaName = _steamFriends.GetPersonaName();
                    Debug.WriteLine($"Retrieved persona name in GetPersonaNameAsync: {_personaName}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error getting persona name: {ex.Message}");
                }
            }

            return _personaName ?? "Unknown User";
        }
        //old version above, new version below
        public async Task<string> GetPersonaNameAsync_OLD_v2()
        {
            if (string.IsNullOrEmpty(_personaName) && _isLoggedIn)
            {
                // Create a new TaskCompletionSource if needed
                _personaNameTcs = new TaskCompletionSource<string>();

                // Request persona information
                if (_steamId != null)
                {
                    _steamFriends.SetPersonaState(EPersonaState.Online);
                    _steamFriends.RequestFriendInfo(_steamId, EClientPersonaStateFlag.PlayerName);
                }

                // Wait for the persona name to be set (with a reasonable timeout)
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(5));
                var completedTask = await Task.WhenAny(_personaNameTcs.Task, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    return "Unknown User"; // Timeout occurred
                }

                return await _personaNameTcs.Task; // Return the actual persona name
            }

            return _personaName ?? "Unknown User";
        }
        //old version above, new version below
        public async Task<string> GetPersonaNameAsync()
        {
            // If we already have a persona name, return it
            if (!string.IsNullOrEmpty(_personaName))
            {
                return _personaName;
            }

            // If we're logged in but don't have a persona name yet
            if (_isLoggedIn && _steamId != null)
            {
                Debug.WriteLine("No persona name yet. Creating TaskCompletionSource and requesting info...");

                // Create a new TaskCompletionSource
                _personaNameTcs = new TaskCompletionSource<string>();

                // Request persona information
                _steamFriends.SetPersonaState(EPersonaState.Online);
                _steamFriends.RequestFriendInfo(_steamId, EClientPersonaStateFlag.PlayerName);

                // Wait for the persona name to be set (with a reasonable timeout for UI responsiveness)
                try
                {
                    var timeoutTask = Task.Delay(TimeSpan.FromSeconds(3));
                    var completedTask = await Task.WhenAny(_personaNameTcs.Task, timeoutTask);

                    if (completedTask == _personaNameTcs.Task)
                    {
                        // We got the persona name
                        return await _personaNameTcs.Task;
                    }

                    Debug.WriteLine("Timed out waiting for persona name");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error waiting for persona name: {ex.Message}");
                }
            }

            return "Unknown User";
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
        private async void OnConnected_OLD(SteamClient.ConnectedCallback callback)
        {
            Debug.WriteLine("Connected to Steam");

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
                Debug.WriteLine($"Authentication error: {ex.Message}");
                _loginTcs.SetResult(false);
            }
        }

        //old version above, new version below
        private async void OnConnected(SteamClient.ConnectedCallback callback)
        {
            Debug.WriteLine("Connected to Steam");

            try
            {
                // This should be turned into a switch statement for different login methods later ...............................................

                if (_isQrCodeLogin && _qrCodeTcs != null)
                {
                    Debug.WriteLine("Handling QR code login");


                    if (_qrCodeTcs == null)
                    {
                        Debug.WriteLine("WARNING: _qrCodeTcs was null in OnConnected, creating a new one");
                        _qrCodeTcs = new TaskCompletionSource<string>();
                    }

                    // PANIC MODE FIX
                    // Now that we're connected, generate the QR code
                    Debug.WriteLine("Generating QR code via BeginAuthSessionViaQRAsync");
                    var authSession = await _steamClient.Authentication.BeginAuthSessionViaQRAsync(new AuthSessionDetails
                    {
                        PlatformType = EAuthTokenPlatformType.k_EAuthTokenPlatformType_MobileApp,
                        ClientOSType = EOSType.Android9,
                        Authenticator = new CustomAuthenticator(null, null)
                    });

                    // Store the auth session for later use
                    _qrAuthSession = authSession;

                    // Get the QR code challenge URL
                    string qrCodeUrl = authSession.ChallengeURL;
                    Debug.WriteLine($"Generated QR code URL: {qrCodeUrl}");

                    // Complete the task with the QR code URL
                    if (_qrCodeTcs != null && !_qrCodeTcs.Task.IsCompleted)
                    {
                        Debug.WriteLine("Setting QR code URL result to _qrCodeTcs");
                        _qrCodeTcs.SetResult(qrCodeUrl);
                    }
                    else
                    {
                        Debug.WriteLine($"Could not set QR code URL: _qrCodeTcs is {(_qrCodeTcs == null ? "null" : "already completed")}");
                    }
                    //// Complete the task with the QR code URL
                    //_qrCodeTcs.SetResult(qrCodeUrl);
                }
                else if (_isSessionKeyLogin)
                {
                    Debug.WriteLine("Handling session key login");

                    // Use the session key for authentication
                    // Note: This is a simplified approach - in a real implementation,
                    // you would need to use the session token to authenticate with Steam

                    // For SteamKit2 v3.0.0, we need to use the token in a different way
                    // This is a placeholder - the actual implementation depends on SteamKit2's API



                    _steamUser.LogOn(new SteamUser.LogOnDetails
                    {
                        Username = _currentUsername, // Use the username extracted from the JSON
                        AccessToken = _sessionKey, // Use the session key as the access token
                        ShouldRememberPassword = false
                    });

                    // Reset the session key login flag
                    _isSessionKeyLogin = false;


                    // Request persona information
                    if (_steamId != null)
                    {
                        Debug.WriteLine($"Requesting persona info for Steam ID: {_steamId}");
                        _steamFriends.SetPersonaState(EPersonaState.Online);
                        _steamFriends.RequestFriendInfo(_steamId, EClientPersonaStateFlag.PlayerName);
                    }
                    else
                    {
                        Debug.WriteLine("Steam ID is null, cannot request persona info");
                    }


                    _loginTcs.SetResult(true);

                    // Reset the session key login flag
                    _isSessionKeyLogin = false;
                }
                else
                {
                    // Existing credential login code
                    var shouldRememberPassword = false;

                    // Begin authenticating via credentials
                    var authSession = await _steamClient.Authentication.BeginAuthSessionViaCredentialsAsync(new AuthSessionDetails
                    {
                        Username = _currentUsername,
                        Password = _currentPassword,
                        IsPersistentSession = shouldRememberPassword,
                        GuardData = null,
                        Authenticator = new CustomAuthenticator(_authCode, _twoFactorCode),
                    });

                    // Starting polling Steam for authentication response
                    var pollResponse = await authSession.PollingWaitForResultAsync();

                    // Logon to Steam with the access token we have received
                    _steamUser.LogOn(new SteamUser.LogOnDetails
                    {
                        Username = pollResponse.AccountName,
                        AccessToken = pollResponse.RefreshToken,
                        ShouldRememberPassword = shouldRememberPassword,
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Authentication error: {ex.Message}");
                Debug.WriteLine($"Exception details: {ex}");

                // Handle errors for the QR code generation
                if (_isQrCodeLogin && _qrCodeTcs != null && !_qrCodeTcs.Task.IsCompleted)
                {
                    Debug.WriteLine($"Setting exception on _qrCodeTcs: {ex.Message}");
                    _qrCodeTcs.SetException(ex);
                }

                if (_loginTcs != null && !_loginTcs.Task.IsCompleted)
                {
                    _loginTcs.SetResult(false);
                }
            }
        }



        private void OnDisconnected(SteamClient.DisconnectedCallback callback)
        {
            Debug.WriteLine("Disconnected from Steam");

            _isLoggedIn = false;

            // If we were logged in, reconnect
            if (_isLoggedIn)
            {
                Debug.WriteLine("Reconnecting to Steam...");
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
                Debug.WriteLine($"Unable to log in to Steam: {callback.Result}");
                Debug.WriteLine($"Extended result: {callback.ExtendedResult}");

                if (callback.Result == EResult.AccountLogonDenied)
                {
                    // Steam Guard is enabled and we need an auth code
                    Debug.WriteLine("This account is protected by Steam Guard. Enter the auth code sent to the associated email.");
                    //_loginTcs.SetResult(false);
                }
                else if (callback.Result == EResult.AccountLoginDeniedNeedTwoFactor)
                {
                    // Two-factor authentication required
                    Debug.WriteLine("This account is protected by Steam Guard Mobile Authenticator. You need to provide the two-factor code from your mobile app.");
                    //_loginTcs.SetResult(false);
                }
                else if (callback.Result == EResult.InvalidPassword)
                {
                    Debug.WriteLine("Invalid password provided.");
                    Debug.WriteLine($"Is using access token: {!string.IsNullOrEmpty(callback.VanityURL)}");
                    //_loginTcs.SetResult(false);
                }

                _loginTcs.SetResult(false);
                return;
            }

            Debug.WriteLine("Successfully logged in to Steam");
            _isLoggedIn = true;
            _steamId = callback.ClientSteamID;


            // Request persona information
            Debug.WriteLine($"Setting persona state and requesting info for {_steamId}");


            // Get persona name after successful login
            // Important: We need to wait a moment for Steam to initialize the friends list
            _steamFriends.SetPersonaState(EPersonaState.Online);
            _steamFriends.RequestFriendInfo(_steamId, EClientPersonaStateFlag.PlayerName); // ??????????

            _loginTcs.SetResult(true);
        }

        private void OnLoggedOff(SteamUser.LoggedOffCallback callback)
        {
            Debug.WriteLine("Logged off of Steam: {0}", callback.Result);
            _isLoggedIn = false;
        }

        private void OnPersonaState_OLD(SteamFriends.PersonaStateCallback callback)
        {
            // This callback is triggered when the Steam network sends information about a user's status.
            // In this case, we're interested in our own status.
            if (callback.FriendID == _steamId)
            {
                _personaName = callback.Name;
                Debug.WriteLine($"Updated persona name: {_personaName}");
            }
        }
        //new version below, old version above
        private void OnPersonaState_OLD_v2(SteamFriends.PersonaStateCallback callback)
        {
            Debug.WriteLine($"Received persona state for: {callback.FriendID}, name: {callback.Name}");

            if (callback.FriendID == _steamId)
            {
                _personaName = callback.Name;
                Debug.WriteLine($"Updated persona name: {_personaName}");

                // Complete the TaskCompletionSource if it exists
                if (_personaNameTcs != null && !_personaNameTcs.Task.IsCompleted)
                {
                    _personaNameTcs.SetResult(_personaName);
                }
            }
            else
            {
                Debug.WriteLine($"Persona state was for different Steam ID. Expected: {_steamId}");
            }
        }
        private void OnPersonaState(SteamFriends.PersonaStateCallback callback)
        {
            Debug.WriteLine($"Received persona state callback. Friend ID: {callback.FriendID}, Name: {callback.Name}");

            if (_steamId != null && callback.FriendID == _steamId)
            {
                Debug.WriteLine($"Matched our Steam ID. Setting persona name to: {callback.Name}");
                _personaName = callback.Name;

                // Complete the TaskCompletionSource if it exists
                if (_personaNameTcs != null && !_personaNameTcs.Task.IsCompleted)
                {
                    _personaNameTcs.SetResult(_personaName);
                }
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

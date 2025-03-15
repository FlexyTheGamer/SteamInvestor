// Models/SteamAuthModel.cs
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using SteamKit2;
using SteamKit2.Authentication;

namespace SteamInventoryAIR.Models
{
    public class SteamAuthModel
    {
        // SteamKit2 components
        private SteamClient _steamClient;
        private SteamUser _steamUser;
        private SteamFriends _steamFriends;
        private CallbackManager _manager;

        // State variables
        private bool _isRunning;
        private bool _isLoggedIn;
        private SteamID _steamId;
        private string _personaName;
        private QrAuthSession _qrAuthSession;

        // Event delegates
        public delegate void ConnectionStatusChangedEventHandler(bool isConnected);
        public delegate void LoginStatusChangedEventHandler(bool isLoggedIn, EResult result);
        public delegate void PersonaStateChangedEventHandler(string personaName);

        // Events
        public event ConnectionStatusChangedEventHandler ConnectionStatusChanged;
        public event LoginStatusChangedEventHandler LoginStatusChanged;
        public event PersonaStateChangedEventHandler PersonaStateChanged;

        public SteamAuthModel()
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

            // Start the callback thread
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

        // Connection methods
        public void Connect()
        {
            _steamClient.Connect();
        }

        public void Disconnect()
        {
            _isLoggedIn = false;
            _steamUser.LogOff();
        }

        // Login methods
        public async Task<AuthSession> BeginCredentialsAuthAsync(string username, string password, string authCode, string twoFactorCode)
        {
            return await _steamClient.Authentication.BeginAuthSessionViaCredentialsAsync(new AuthSessionDetails
            {
                Username = username,
                Password = password,
                IsPersistentSession = false,
                GuardData = null,
                Authenticator = new CustomAuthenticator(authCode, twoFactorCode)
            });
        }

        public async Task<QrAuthSession> BeginQRAuthAsync()
        {
            _qrAuthSession = await _steamClient.Authentication.BeginAuthSessionViaQRAsync(new AuthSessionDetails());
            return _qrAuthSession;
        }

        public void LogOnWithToken(string username, string accessToken)
        {
            _steamUser.LogOn(new SteamUser.LogOnDetails
            {
                Username = username,
                AccessToken = accessToken,
                ShouldRememberPassword = false
            });
        }

        // Status methods
        public bool IsLoggedIn() => _isLoggedIn;
        public string GetPersonaName() => _personaName;
        public SteamID GetSteamID() => _steamId;

        // Persona methods
        public void RequestPersonaInfo()
        {
            if (_isLoggedIn && _steamId != null)
            {
                _steamFriends.SetPersonaState(EPersonaState.Online);
                _steamFriends.RequestFriendInfo(_steamId, EClientPersonaStateFlag.PlayerName);
            }
        }

        // Callback handlers
        private void OnConnected(SteamClient.ConnectedCallback callback)
        {
            ConnectionStatusChanged?.Invoke(true);
        }

        private void OnDisconnected(SteamClient.DisconnectedCallback callback)
        {
            _isLoggedIn = false;
            ConnectionStatusChanged?.Invoke(false);
        }

        private void OnLoggedOn(SteamUser.LoggedOnCallback callback)
        {
            _isLoggedIn = callback.Result == EResult.OK;
            _steamId = callback.ClientSteamID;

            if (_isLoggedIn)
            {
                RequestPersonaInfo();
            }

            LoginStatusChanged?.Invoke(_isLoggedIn, callback.Result);
        }

        private void OnLoggedOff(SteamUser.LoggedOffCallback callback)
        {
            _isLoggedIn = false;
            LoginStatusChanged?.Invoke(false, callback.Result);
        }

        private void OnPersonaState(SteamFriends.PersonaStateCallback callback)
        {
            if (_steamId != null && callback.FriendID == _steamId)
            {
                _personaName = callback.Name;
                PersonaStateChanged?.Invoke(_personaName);
            }
        }

        // Helper classes
        private class CustomAuthenticator : IAuthenticator
        {
            private readonly string _authCode;
            private readonly string _twoFactorCode;

            public CustomAuthenticator(string authCode, string twoFactorCode)
            {
                _authCode = authCode;
                _twoFactorCode = twoFactorCode;
            }

            public Task<string> GetDeviceCodeAsync(bool previousCodeWasIncorrect) =>
                Task.FromResult(_authCode);

            public Task<string> GetEmailCodeAsync(string email, bool previousCodeWasIncorrect) =>
                Task.FromResult(_authCode);

            public Task<string> GetTwoFactorCodeAsync(bool previousCodeWasIncorrect) =>
                Task.FromResult(_twoFactorCode);

            public Task<bool> AcceptDeviceConfirmationAsync() =>
                Task.FromResult(true);
        }
    }
}

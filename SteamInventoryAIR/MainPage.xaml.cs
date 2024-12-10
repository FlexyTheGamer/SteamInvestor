using SteamKit2;
using SteamKit2.Authentication;
using System.Diagnostics;

namespace SteamInventoryAIR
{
    public partial class MainPage : ContentPage
    {
        int count = 0;
        private SteamClient steamClient;
        private CallbackManager callbackManager;
        private SteamUser steamUser;

        string user = "ivan30123";
        string pass = "EFrqxuEFHpYs2dmQKzNX";
        string previouslyStoredGuardData = null; // For the sake of this sample, we do not persist guard data
        bool isRunning = true;

        public MainPage()
        {
            InitializeComponent();

            steamClient = new SteamClient();
            callbackManager = new CallbackManager(steamClient);
            steamUser = steamClient.GetHandler<SteamUser>();

            // Register Callbacks
            callbackManager.Subscribe<SteamClient.ConnectedCallback>(OnConnected);
            callbackManager.Subscribe<SteamClient.DisconnectedCallback>(OnDisconnected);
            callbackManager.Subscribe<SteamUser.LoggedOnCallback>(OnLoggedOn);
            callbackManager.Subscribe<SteamUser.LoggedOffCallback>(OnLoggedOff);

            
            Task.Run(() => ConnectToSteam());
        }


        private void OnCounterClicked(object sender, EventArgs e)
        {
            count++;

            if (count == 1)
            {
                CounterBtn.Text = $"Clicked {count} time";
            }
            else
                CounterBtn.Text = $"Clicked {count} times";

            SemanticScreenReader.Announce(CounterBtn.Text);
        }

        private void ConnectToSteam()
        {
            steamClient.Connect();

            while (isRunning)
            {
                callbackManager.RunWaitCallbacks(TimeSpan.FromMilliseconds(100));
            }
        }

        async void OnConnected(SteamClient.ConnectedCallback callback)
        {
            Trace.WriteLine( "Connected to Steam! Logging in '{0}'...", user );

            var shouldRememberPassword = false;

            // Begin authenticating via credentials
            var authSession = await steamClient.Authentication.BeginAuthSessionViaCredentialsAsync( new AuthSessionDetails
            {
                Username = user,
                Password = pass,
                IsPersistentSession = shouldRememberPassword,

                // See NewGuardData comment below
                GuardData = previouslyStoredGuardData,

                /// <see cref="UserConsoleAuthenticator"/> is the default authenticator implemention provided by SteamKit
                /// for ease of use which blocks the thread and asks for user input to enter the code.
                /// However, if you require special handling (e.g. you have the TOTP secret and can generate codes on the fly),
                /// you can implement your own <see cref="SteamKit2.Authentication.IAuthenticator"/>.
                Authenticator = new UserConsoleAuthenticator(),
            } );

            // Starting polling Steam for authentication response
            var pollResponse = await authSession.PollingWaitForResultAsync();

            if ( pollResponse.NewGuardData != null )
            {
                // When using certain two factor methods (such as email 2fa), guard data may be provided by Steam
                // for use in future authentication sessions to avoid triggering 2FA again (this works similarly to the old sentry file system).
                // Do note that this guard data is also a JWT token and has an expiration date.
                previouslyStoredGuardData = pollResponse.NewGuardData;
            }

            // Logon to Steam with the access token we have received
            // Note that we are using RefreshToken for logging on here
            steamUser.LogOn( new SteamUser.LogOnDetails
            {
                Username = pollResponse.AccountName,
                AccessToken = pollResponse.RefreshToken,
                ShouldRememberPassword = shouldRememberPassword, // If you set IsPersistentSession to true, this also must be set to true for it to work correctly
            } );
        }

        private void OnDisconnected(SteamClient.DisconnectedCallback callback)
        {
            // Handle disconnection
            Trace.WriteLine("Disconnected from Steam");
        }

        private void OnLoggedOn(SteamUser.LoggedOnCallback callback)
        {
            if ( callback.Result != EResult.OK )
            {
                //Trace.WriteLine( "Unable to logon to Steam: {0} / {1}", callback.Result, callback.ExtendedResult );

                isRunning = false;
                return;
            }

            Trace.WriteLine( "Successfully logged on!" );

            // at this point, we'd be able to perform actions on Steam

            // for this sample we'll just log off
            steamUser.LogOff();
        }

        private void OnLoggedOff(SteamUser.LoggedOffCallback callback)
        {
            Trace.WriteLine($"Logged off: {callback.Result}");
        }
    }

}

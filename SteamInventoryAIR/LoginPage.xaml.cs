namespace SteamInventoryAIR;

using Microsoft.Maui.Controls;

using SteamInventoryAIR.ViewModels;

using Microsoft.Maui.ApplicationModel;
using System.Diagnostics;

public partial class LoginPage : ContentPage
{
    private readonly LoginViewModel _viewModel;

    // Track current login method
    private enum LoginMethod
    {
        Traditional,
        SessionKey,
        QRCode
    }

    private LoginMethod _currentMethod = LoginMethod.Traditional;

    public LoginPage(LoginViewModel viewModel)    //Changed Public -> Internal (because of an error)     (argument -> part of add for MVVM Architecture)
    {
        InitializeComponent(); //x:Class value must match your namespace and class name exactly.

        _viewModel = viewModel;         //Added for MVVM Architecture
        BindingContext = _viewModel;    //Added for MVVM Architecture

        // Set default login method
        SwitchLoginMethod(LoginMethod.Traditional);
    }


    private void SwitchLoginMethod(LoginMethod method)
    {
        // Hide all templates first
        TraditionalLoginTemplate.IsVisible = false;
        SessionKeyLoginTemplate.IsVisible = false;
        QRCodeLoginTemplate.IsVisible = false;

        // Reset the login status
        _viewModel.LoginStatus = string.Empty;

        // Show the selected template
        switch (method)
        {
            case LoginMethod.Traditional:
                TraditionalLoginTemplate.IsVisible = true;
                _viewModel.CurrentMethod = LoginViewModel.LoginMethod.Traditional;
                break;
            case LoginMethod.SessionKey:
                SessionKeyLoginTemplate.IsVisible = true;
                _viewModel.CurrentMethod = LoginViewModel.LoginMethod.SessionKey;
                break;
            case LoginMethod.QRCode:
                QRCodeLoginTemplate.IsVisible = true;
                _viewModel.CurrentMethod = LoginViewModel.LoginMethod.QRCode;
                // Generate QR code when this method is selected
                _viewModel.GenerateQrCodeCommand.Execute(null);
                break;
        }

        _currentMethod = method;


        //New Ver
        SteamLoginButton.BackgroundColor = method == LoginMethod.Traditional
            ? Color.FromArgb("#CC2424") : Color.FromArgb("#2A2E35");
        SteamLoginButton.TextColor = method == LoginMethod.Traditional
            ? Colors.White : Color.FromArgb("#CCCCCC");

        WebSessionButton.BackgroundColor = method == LoginMethod.SessionKey
            ? Color.FromArgb("#CC2424") : Color.FromArgb("#2A2E35");
        WebSessionButton.TextColor = method == LoginMethod.SessionKey
            ? Colors.White : Color.FromArgb("#CCCCCC");

        QRCodeButton.BackgroundColor = method == LoginMethod.QRCode
            ? Color.FromArgb("#CC2424") : Color.FromArgb("#2A2E35");
        QRCodeButton.TextColor = method == LoginMethod.QRCode
            ? Colors.White : Color.FromArgb("#CCCCCC");
    }

    // Button click handlers for switching login methods
    private void OnSteamLoginClicked(object sender, EventArgs e)
    {
        SwitchLoginMethod(LoginMethod.Traditional);
    }

    private void OnWebSessionClicked(object sender, EventArgs e)
    {
        SwitchLoginMethod(LoginMethod.SessionKey);
    }

    private void OnQRCodeClicked(object sender, EventArgs e)
    {
        SwitchLoginMethod(LoginMethod.QRCode);
    }

    private async void OnHelpButtonClicked(object sender, EventArgs e)
    {
        bool openBrowser = await DisplayAlert("Steam Session Key Help",
                "To find your Steam session key:\n\n" +
                "1. Click 'Open in Browser' to go to the Steam session key page\n" +
                "2. Make sure you're logged into Steam in your browser\n" +
                "3. Copy the entire JSON response or just the 'token' value\n" +
                "4. Paste it back here to log in",
                "Open in Browser", "Cancel");

        if (openBrowser)
        {
            await Browser.OpenAsync(new Uri("https://steamcommunity.com/chat/clientjstoken"), BrowserLaunchMode.SystemPreferred);
        }
    }

    //This apprently has a better way to be implemented with ICommand - try it later if you have more time...
    private async void OnPasteSessionKeyClicked(object sender, EventArgs e)
    {
        string clipboardText = await Clipboard.GetTextAsync();
        if (!string.IsNullOrEmpty(clipboardText))
        {

            //SessionKeyEntry.Text = clipboardText;             //Pre MVVM Architecture
            _viewModel.SessionKey = clipboardText;              //Added for MVVM Architecture

        }
    }

    private async void OnScanQRCodeClicked(object sender, EventArgs e)
    {
        // Implement camera scanning functionality
        // This would require a camera plugin
        await DisplayAlert("Scan QR", "Camera functionality will be implemented later", "OK");
    }

    // In LoginPage.xaml.cs - add this in the OnAppearing method
    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Load debug credentials after the page is visible
        Debug.WriteLine("LoginPage OnAppearing - reloading debug credentials");
        _ = _viewModel.LoadDebugCredentialsAsync();
    }

}
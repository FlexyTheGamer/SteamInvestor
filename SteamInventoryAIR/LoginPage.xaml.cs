namespace SteamInventoryAIR;

using Microsoft.Maui.Controls;

using SteamInventoryAIR.ViewModels;

using Microsoft.Maui.ApplicationModel;

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

        // Show the selected template
        switch (method)
        {
            case LoginMethod.Traditional:
                TraditionalLoginTemplate.IsVisible = true;
                break;
            case LoginMethod.SessionKey:
                SessionKeyLoginTemplate.IsVisible = true;
                break;
            case LoginMethod.QRCode:
                QRCodeLoginTemplate.IsVisible = true;
                // Generate QR code when this method is selected

                //GenerateQRCode();                                 //Pre MVVM Architecture
                _viewModel.GenerateQrCodeCommand.Execute(null);     //Added for MVVM Architecture

                break;
        }

        _currentMethod = method;

        // Update button states to show which is selected

        //Old Ver
        //SteamLoginButton.BackgroundColor = method == LoginMethod.Traditional
        //    ? Colors.LightBlue : Colors.Gray;
        //WebSessionButton.BackgroundColor = method == LoginMethod.SessionKey
        //    ? Colors.LightBlue : Colors.Gray;
        //QRCodeButton.BackgroundColor = method == LoginMethod.QRCode
        //    ? Colors.LightBlue : Colors.Gray;

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

    // Login button handlers - Pre MVVM Architecture

    //private async void OnLoginButtonClicked(object sender, EventArgs e)
    //{
    //    if (string.IsNullOrEmpty(UsernameEntry.Text) || string.IsNullOrEmpty(PasswordEntry.Text))
    //    {
    //        await DisplayAlert("Error", "Username and password are required", "OK");
    //        return;
    //    }

    //    // Here you would implement the actual Steam login logic using SteamKit2
    //    // For now, we'll just show a success message and navigate to the main page
    //    await DisplayAlert("Login", $"Logging in with username: {UsernameEntry.Text}", "OK");
    //    await Shell.Current.GoToAsync("///MainPage");
    //}

    //private async void OnSessionKeyLoginClicked(object sender, EventArgs e)
    //{
    //    if (string.IsNullOrEmpty(SessionKeyEntry.Text))
    //    {
    //        await DisplayAlert("Error", "Session key is required", "OK");
    //        return;
    //    }

    //    // Here you would implement the actual session key login logic
    //    // For now, we'll just show a success message and navigate to the main page
    //    await DisplayAlert("Login", "Logging in with session key", "OK");
    //    await Shell.Current.GoToAsync("///MainPage");
    //}

    // Pre MVVM Architecture
    //private async void OnQRLoginClicked(object sender, EventArgs e)
    //{
    //    // This would be triggered after QR code is scanned
    //    // For now, we'll just show a message and navigate to the main page
    //    await DisplayAlert("QR Login", "QR login successful", "OK");
    //    await Shell.Current.GoToAsync("///MainPage");
    //}

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

    // Pre MVVM Architecture
    //private void GenerateQRCode()
    //{
    //    // In a real implementation, you would:
    //    // 1. Call Steam API to generate a login token
    //    // 2. Create a QR code from that token
    //    // 3. Display it in the QRCodeImage

    //    // For now, we'll just use a placeholder
    //    QRCodeImage.Source = "qr_code_placeholder.png";
    //}




}
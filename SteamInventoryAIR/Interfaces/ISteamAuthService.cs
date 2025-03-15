using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamInventoryAIR.Interfaces
{
    public interface ISteamAuthService
    {
        Task<bool> LoginWithCredentialsAsync(string username, string password, string? authCode = null);
        Task<bool> LoginWithSessionKeyAsync(string sessionKey);
        Task<bool> LoginWithQRCodeAsync(string qrToken);
        Task<string> GenerateQRCodeTokenAsync();
        Task<bool> IsLoggedInAsync();
        Task LogoutAsync();

        Task<string> GetPersonaNameAsync();

        Task<bool> SubmitTwoFactorCodeAsync(string twoFactorCode);

    }
}

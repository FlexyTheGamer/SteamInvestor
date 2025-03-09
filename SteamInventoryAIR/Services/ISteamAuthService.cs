using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamInventoryAIR.Services
{
    internal interface ISteamAuthService
    {
        Task<bool> LoginWithCredentialsAsync(string username, string password, string authCode = null);
        Task<bool> LoginWithSessionKeyAsync(string sessionKey);
        Task<bool> LoginWithQRCodeAsync(string qrToken);
        Task<string> GenerateQRCodeTokenAsync();
        Task<bool> IsLoggedInAsync();
        Task LogoutAsync();
    }
}

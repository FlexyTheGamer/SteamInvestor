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
        Task<bool> LogoutAsync();

        Task<string> GetPersonaNameAsync();

        Task<bool> SubmitTwoFactorCodeAsync(string twoFactorCode);


        Task<IEnumerable<Models.InventoryItem>> GetInventoryAsync(uint appId = 730, uint contextId = 2);

        //Task<IEnumerable<Models.InventoryItem>> GetCompleteInventoryAsync(uint appId = 730, uint contextId = 2);

        Task<IEnumerable<Models.InventoryItem>> GetInventoryViaWebAPIAsync(uint appId = 730, uint contextId = 2);



    }
}

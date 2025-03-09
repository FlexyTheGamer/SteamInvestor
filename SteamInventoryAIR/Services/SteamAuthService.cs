using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamInventoryAIR.Interfaces;

namespace SteamInventoryAIR.Services
{
    public  class SteamAuthService : ISteamAuthService
    {
        public Task<bool> LoginWithCredentialsAsync(string username, string password, string? authCode = null)
        {
            throw new NotImplementedException();
        }

        public Task<bool> LoginWithSessionKeyAsync(string sessionKey)
        {
            throw new NotImplementedException();
        }

        public Task<bool> LoginWithQRCodeAsync(string qrToken)
        {
            throw new NotImplementedException();
        }

        public Task<string> GenerateQRCodeTokenAsync()
        {
            throw new NotImplementedException();
        }

        public Task<bool> IsLoggedInAsync()
        {
            throw new NotImplementedException();
        }

        public Task LogoutAsync()
        {
            throw new NotImplementedException();
        }
    }
}

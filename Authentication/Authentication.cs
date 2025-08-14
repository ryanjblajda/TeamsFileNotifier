using Microsoft.Identity.Client;
using Serilog;
using System.Security.Cryptography;
using TeamsFileNotifier.Global;

namespace TeamsFileNotifier.Authentication
{
    internal static class Authentication
    {
        private static readonly string[] Scopes = new[] { "User.Read", "Group.Read.All", "ChannelMessage.Send", "ChannelMessage.Read.All" };
        private const string TenantId = "43144288-676d-457c-a9a7-f271a812b8ac"; // e.g., 72f988bf-xxxx-xxxx-xxxx-2d7cd011db47
        private static readonly string Authority = $"https://login.microsoftonline.com/{TenantId}";

        private static void ConfigureTokenCache(ITokenCache tokenCache)
        {
            string tokenFileFullPath = Path.Combine(Functions.GetDefaultTempPathLocation(Log.Logger), Values.DefaultTokenCacheFilename);
            Log.Information(tokenFileFullPath);

            tokenCache.SetBeforeAccess(args =>
            {
                lock (tokenCache)
                {
                    if (File.Exists(tokenFileFullPath)) 
                    {
                        var protectedData = File.ReadAllBytes(tokenFileFullPath);
                        var data = ProtectedData.Unprotect(protectedData, null, DataProtectionScope.CurrentUser);
                        args.TokenCache.DeserializeMsalV3(data);
                        Log.Information("Loading Current User Token Cache");
                    }
                }
            });

            tokenCache.SetAfterAccess(args =>
            {
                if (args.HasStateChanged)
                {
                    lock (tokenCache)
                    {
                        Directory.CreateDirectory(Functions.GetDefaultTempPathLocation(Log.Logger)!);
                        var data = args.TokenCache.SerializeMsalV3();
                        var protectedData = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
                        File.WriteAllBytes(tokenFileFullPath, protectedData);
                        Log.Information("Writing Current User Cache");
                    }
                }
            });
        }

        internal static async void StartAuthentication()
        {
            var app = PublicClientApplicationBuilder.Create("a18e17ec-975e-423e-a706-a2a5d95e993e").WithAuthority(Authority).WithRedirectUri("http://localhost/").Build();
            
            ConfigureTokenCache(app.UserTokenCache);

            AuthenticationResult result = null;

            var accounts = await app.GetAccountsAsync();
            var firstAccount = accounts.FirstOrDefault();

            try
            {
                // Try to get token silently (from cache)
                result = await app.AcquireTokenSilent(Scopes, firstAccount).ExecuteAsync();
                Log.Information("Token acquired silently.");
            }
            catch (MsalUiRequiredException)
            {
                // No token cached or expired, so prompt user login
                result = await app.AcquireTokenInteractive(Scopes).WithPrompt(Prompt.SelectAccount).ExecuteAsync();

                Log.Information("Token acquired interactively.");
            }

            Values.AccessToken = result.AccessToken;
        }
    }
}

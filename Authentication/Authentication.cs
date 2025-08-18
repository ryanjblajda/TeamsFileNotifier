using Microsoft.Identity.Client;
using Serilog;
using TeamsFileNotifier.Messaging;
using System.Security.Cryptography;
using TeamsFileNotifier.Global;

namespace TeamsFileNotifier.Authentication
{
    internal static class Authentication
    {
        private static readonly System.Threading.Timer tokenExpirationTimer = new System.Threading.Timer(OnTokenExpired);
        private static readonly string[] Scopes = new[] { "User.Read", "Group.Read.All", "ChannelMessage.Send", "ChannelMessage.Read.All" };
        private const string TenantId = "43144288-676d-457c-a9a7-f271a812b8ac"; // e.g., 72f988bf-xxxx-xxxx-xxxx-2d7cd011db47
        private static readonly string Authority = $"https://login.microsoftonline.com/{TenantId}";
        private static readonly IPublicClientApplication _app;

        static Authentication()
        {
            Values.MessageBroker.Subscribe<AuthenticationFailureMessage>(OnAuthenticationFailed);
            _app = PublicClientApplicationBuilder.Create("a18e17ec-975e-423e-a706-a2a5d95e993e").WithAuthority(Authority).WithRedirectUri("http://localhost/").Build();
            ConfigureTokenCache(_app.UserTokenCache);
        }

        private static void OnAuthenticationFailed(AuthenticationFailureMessage message)
        {
            Log.Warning("Authentication | received an authentication failure message, so we will attempt to authenticate");
            AuthenticationRoutine();
        }

        private static void ConfigureTokenCache(ITokenCache tokenCache)
        {
            string tokenFileFullPath = Path.Combine(Functions.GetDefaultTempPathLocation(Log.Logger), Values.DefaultTokenCacheFilename);
            Log.Information($"Authentication | user token cache located @ {tokenFileFullPath}");

            tokenCache.SetBeforeAccess(args =>
            {
                lock (tokenCache)
                {
                    if (File.Exists(tokenFileFullPath)) 
                    {
                        var protectedData = File.ReadAllBytes(tokenFileFullPath);
                        var data = ProtectedData.Unprotect(protectedData, null, DataProtectionScope.CurrentUser);
                        args.TokenCache.DeserializeMsalV3(data);
                        Log.Information("Authentication | Loading Current User Token Cache");
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
                        Log.Information("Authentication | Writing Current User Cache");
                    }
                }
            });
        }

        internal static void OnTokenExpired(object? state)
        {
            Log.Information("Authentication | token about to expire, firing authentication routine to renew token");
            AuthenticationRoutine();
        }

        internal static async Task<AuthenticationResult> GetAuthenticationTokenSilent(IPublicClientApplication app, IAccount account)
        {
            // Try to get token silently (from cache)
            var result = await app.AcquireTokenSilent(Scopes, account).ExecuteAsync();
            Log.Information($"Authentication | token acquired silently -> expires @ {result.ExpiresOn.LocalDateTime}");

            return result;
        }

        internal static async Task<AuthenticationResult> GetAuthenticationTokenInteractive(IPublicClientApplication app)
        {
            // No token cached or expired, so prompt user login
            var result = await app.AcquireTokenInteractive(Scopes).WithPrompt(Prompt.SelectAccount).ExecuteAsync();
            //log
            Log.Information($"Authentication | token acquired interactively -> expires @ {result.ExpiresOn.LocalDateTime}");

            return result;
        }

        internal static async Task<IAccount?> GetUsersFirstAccount(IPublicClientApplication app)
        {
            var accounts = await app.GetAccountsAsync();
            return accounts.FirstOrDefault();
        }

        private static long GetDueTime(DateTimeOffset offset)
        {
            //default to 1 hr
            long result = 1000 * 60 * 60;
            //determine the time until expiration of the token
            TimeSpan timeUntilExpiry = offset - DateTimeOffset.UtcNow;
            //if the token has already expired, we will set the time until expiry to zero, so that the token is refreshed immediately
            if (timeUntilExpiry < TimeSpan.Zero) { timeUntilExpiry = TimeSpan.Zero; }
            //convert to the long, minus 1 second from the expiration time
            result = (long)timeUntilExpiry.TotalMilliseconds - 1000;
            Log.Information($"Authentication | the token will expire in {timeUntilExpiry.TotalMilliseconds}ms, so we will have the timer fire in {result}ms");
            //return the result
            return result;
        }

        internal static async void AuthenticationRoutine()
        { 
            AuthenticationResult? result = null;

            var firstAccount = await GetUsersFirstAccount(_app);

            //if the account not null, then we should be able to silently retreive a token
            if (firstAccount != null)
            {
                try { result = await GetAuthenticationTokenSilent(_app, firstAccount); }
                catch (Exception e) { Log.Fatal($"Authentication | unable to silently retrieve token -> {e.Message}"); }
            }
            else
            {
                //catch any exceptions, but in theory since we know the account is null, we shouldnt fire any
                try { result = await GetAuthenticationTokenInteractive(_app); }
                //log the fatal error
                catch (Exception e) { Log.Fatal($"Authentication | exception encountered attempting to interactively retrieve a token -> {e.Message}"); }
            }

            if (result != null) { 
                //store the token
                Values.AccessToken = result.AccessToken;
                //restart the timer so the token is refreshed when needed
                tokenExpirationTimer.Change(GetDueTime(result.ExpiresOn), Timeout.Infinite);
                Log.Information("Authentication | starting token refresh timer");
            }
            else { Log.Error("Authentication | unable to store token because it is null"); }
        }
    }
}

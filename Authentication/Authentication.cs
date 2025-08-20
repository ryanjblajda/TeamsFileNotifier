using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Broker;
using Serilog;
using TeamsFileNotifier.Messaging;
using System.Security.Cryptography;
using TeamsFileNotifier.Global;
using Serilog.Events;

namespace TeamsFileNotifier.Authentication
{
    internal static class Authentication
    {
        private static readonly System.Threading.Timer tokenExpirationTimer = new System.Threading.Timer(OnTokenExpired);
        private static readonly string[] Scopes = new[] { "User.Read", "User.ReadBasic.All", "ChannelMessage.Send" };
        private const string TenantId = "43144288-676d-457c-a9a7-f271a812b8ac"; // e.g., 72f988bf-xxxx-xxxx-xxxx-2d7cd011db47
        private static readonly string Authority = $"https://login.microsoftonline.com/{TenantId}";
        private static readonly IPublicClientApplication _app;

        static Authentication()
        {
            Values.MessageBroker.Subscribe<AuthenticationFailureMessage>(OnAuthenticationFailed);
            // optional, enables login with current Windows account
            // will also prevent outlook from logging out by using the shared cache, and forcing a logout when user logs out of other accounts
            BrokerOptions brokerOptions = new BrokerOptions(BrokerOptions.OperatingSystems.Windows) { ListOperatingSystemAccounts = true };
            //only log warnings when running normally
            LogLevel level = LogLevel.Warning;
            //if compiled with debug show ALL the things
            #if DEBUG
                level = LogLevel.Verbose;
            #endif
            
            _app = PublicClientApplicationBuilder.Create("a18e17ec-975e-423e-a706-a2a5d95e993e")
                .WithBroker(brokerOptions).
                WithAuthority(Authority).
                WithRedirectUri("http://localhost/").
                WithLogging(LogCallback, level, enablePiiLogging: true, enableDefaultPlatformLogging: false).
                Build();
            //removed to prevent overwriting WAM cache
            //ConfigureTokenCache(_app.UserTokenCache);
        }

        private static void LogCallback(LogLevel level, string message, bool containsPii)
        {
            // Optionally ignore PII in logs
            if (containsPii)
            {
                Log.Warning("Authentication | Response Contains PII");
                //return;
            }

            LogEventLevel serilogLevel = level switch
            {
                LogLevel.Error => LogEventLevel.Error,
                LogLevel.Warning => LogEventLevel.Warning,
                LogLevel.Info => LogEventLevel.Information,
                LogLevel.Verbose => LogEventLevel.Debug,
                _ => LogEventLevel.Information
            };

            Log.Write(serilogLevel, "MSAL | {message}", message);
        }

        private static void OnAuthenticationFailed(AuthenticationFailureMessage message)
        {
            Log.Warning("Authentication | received an authentication failure message, so we will attempt to authenticate");
            AuthenticationRoutine();
        }

        /// <summary>
        /// DEPRECATED - didnt realize that this would cause logouts in other apps
        /// </summary>
        /// <param name="tokenCache"></param>
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

        internal static async Task<AuthenticationResult?> GetAuthenticationTokenInteractive(IPublicClientApplication app)
        {
            AuthenticationResult? result = null;

            //show a notification balloon
            Values.MessageBroker.Publish(new BalloonMessage("interactive login required", "Please Login", "A window should open momentarily, please log into your CCS New England account to continue.", ToolTipIcon.Info));
            // No token cached or expired, so prompt user login
            using (var hiddenForm = new Form())
            {
                hiddenForm.StartPosition = FormStartPosition.Manual;
                hiddenForm.Size = new Size(1, 1);      // tiny
                hiddenForm.ShowInTaskbar = false;
                hiddenForm.Opacity = 0;                // fully invisible
                hiddenForm.Show();

                result = await app.AcquireTokenInteractive(Scopes).WithParentActivityOrWindow(hiddenForm.Handle).ExecuteAsync();
            }
            //log the result
            Log.Information($"Authentication | {(result == null ? "token acquisition failure" : "token acquired interactively")} -> expires @ {result?.ExpiresOn.LocalDateTime}");

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

            bool tryInteractive = false;

            //if the account not null, then we should be able to silently retreive a token
            if (firstAccount != null)
            {
                try { result = await GetAuthenticationTokenSilent(_app, firstAccount); }
                catch (Exception e) {
                    tryInteractive = true;
                    Log.Fatal($"Authentication | unable to silently retrieve token -> {e.Message}"); 
                }
            }

            if (firstAccount != null && tryInteractive) { 
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
            else { Log.Error("Authentication | not storing token because it is null"); }
        }
    }
}

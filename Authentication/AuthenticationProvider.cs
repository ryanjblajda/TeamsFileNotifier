using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;
using TeamsFileNotifier.Global;

namespace TeamsFileNotifier.Authentication
{
    internal class AuthenticationProvider : IAuthenticationProvider
    {
        public Task AuthenticateRequestAsync(RequestInformation request, Dictionary<string, object>? additionalAuthenticationContext = null, CancellationToken cancellationToken = default)
        {
            request.Headers.Add("Authorization", $"Bearer {Values.AccessToken}");
            return Task.CompletedTask;
        }
    }
}

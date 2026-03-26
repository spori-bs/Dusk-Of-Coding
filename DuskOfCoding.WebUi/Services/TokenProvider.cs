using Microsoft.AspNetCore.Components.Authorization;
using System.Threading.Tasks;

namespace DuskOfCoding.WebUi.Services;

public class TokenProvider
{
    private readonly AuthenticationStateProvider _authStateProvider;

    public TokenProvider(AuthenticationStateProvider authStateProvider)
    {
        _authStateProvider = authStateProvider;
    }

    public async Task<string?> GetAccessTokenAsync()
    {
        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        return authState.User.FindFirst("access_token")?.Value;
    }
}

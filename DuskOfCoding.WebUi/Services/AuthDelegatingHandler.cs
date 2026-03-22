using System.Net;
using Microsoft.AspNetCore.Components;

namespace DuskOfCoding.WebUi.Services;

public class AuthDelegatingHandler : DelegatingHandler
{
    private readonly NavigationManager _navigationManager;

    public AuthDelegatingHandler(NavigationManager navigationManager)
    {
        _navigationManager = navigationManager;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            var returnUrl = Uri.EscapeDataString(new Uri(_navigationManager.Uri).PathAndQuery);
            _navigationManager.NavigateTo($"/login?returnUrl={returnUrl}", forceLoad: true);
        }
        else if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            _navigationManager.NavigateTo("/access-denied", forceLoad: true);
        }

        return response;
    }
}

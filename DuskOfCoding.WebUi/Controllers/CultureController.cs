using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace DuskOfCoding.WebUi.Controllers;

/// <summary>
/// Controller to handle culture switching via cookie.
/// Called from the LanguageSelector component.
/// </summary>
[Route("[controller]/[action]")]
public class CultureController : Controller
{
    public IActionResult Set(string culture, string redirectUri)
    {
        HttpContext.Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true,
                SameSite = SameSiteMode.Lax
            });

        return LocalRedirect(redirectUri);
    }
}

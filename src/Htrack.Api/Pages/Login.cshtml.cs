using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace HTrack.Api.Pages;

public class LoginModel(IConfiguration config) : PageModel
{
    public string? Error { get; private set; }

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToPage("/Admin/Companies/Index");
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string username, string password)
    {
        var expectedUser = config["AdminCredentials:Username"];
        var expectedPass = config["AdminCredentials:Password"];

        if (username == expectedUser && password == expectedPass)
        {
            var claims = new List<Claim> { new(ClaimTypes.Name, username) };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity));
            return RedirectToPage("/Admin/Companies/Index");
        }

        Error = "Noto'g'ri login yoki parol";
        return Page();
    }

    public async Task<IActionResult> OnPostLogoutAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToPage("/Login");
    }
}

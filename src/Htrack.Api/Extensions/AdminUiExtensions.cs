using Microsoft.AspNetCore.Authentication.Cookies;

namespace HTrack.Api.Extensions;

internal static class AdminUiExtensions
{
    internal static IServiceCollection AddAdminUi(this IServiceCollection services)
    {
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(o => { o.LoginPath = "/Login"; o.AccessDeniedPath = "/Login"; });

        services.AddRazorPages(o => o.Conventions.AuthorizeFolder("/Admin"));

        return services;
    }
}

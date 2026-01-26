using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Ecommerce.MVC.Helpers
{
    public class CartTokenHelper
    {
        private const string CookieName = "cart_token";

    public static string GetOrCreateToken(HttpContext http)
    {
        if (http.Request.Cookies.TryGetValue(CookieName, out var token) && !string.IsNullOrWhiteSpace(token))
            return token;

        // token forte (URL-safe base64)
        var bytes = RandomNumberGenerator.GetBytes(24);
        var newToken = Convert.ToBase64String(bytes)
            .Replace("+", "-").Replace("/", "_").TrimEnd('=');

        http.Response.Cookies.Append(CookieName, newToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = http.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(7)
        });

        return newToken;
    }
    }
}
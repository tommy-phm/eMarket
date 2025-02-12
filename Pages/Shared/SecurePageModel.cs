using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

// Secured Pages requires login
public class SecurePageModel : PageModel
{
    public string? Username { get; private set; }
    public bool IsAuthenticated { get; private set; }

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        var token = Request.Cookies["AuthToken"];

        if (string.IsNullOrEmpty(token))
        {
            context.Result = new RedirectResult("/Login");
            return;
        }

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            if (jwtToken.ValidTo < DateTime.UtcNow)
            {
                context.Result = new RedirectResult("/Login");
                return;
            }

            Username = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
            IsAuthenticated = true;
        }
        catch
        {
            context.Result = new RedirectResult("/Login");
        }
    }
}


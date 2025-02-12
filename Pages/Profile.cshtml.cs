using Microsoft.AspNetCore.Mvc;

namespace eMarket.Pages
{
    public class ProfileModel : SecurePageModel
    {
        public IActionResult OnGet()
        {
            if (!IsAuthenticated)
            {
                return Redirect("/login"); 
            }
            return Page();
        }
    }
}
